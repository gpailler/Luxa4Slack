namespace CG.Luxa4Slack
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;

  using NLog;

  using SlackAPI;
  using SlackAPI.RPCMessages;
  using SlackAPI.WebSocketMessages;

  internal class SlackNotificationAgent : IDisposable
  {
    private const int Timeout = 15000;
    private const int HistoryItemsToFetch = 50;

    protected readonly ILogger Logger = LogManager.GetLogger("Slack");
    protected SlackSocketClient Client;

    private readonly string token;
    private readonly ChannelsInfo channelsInfo = new ChannelsInfo();
    private readonly List<string> highlightWords = new List<string>();

    private delegate void GetHistoryHandler(Action<MessageHistory> callback, Channel groupInfo, DateTime? latest, DateTime? oldest, int? count);

    public SlackNotificationAgent(string token)
    {
      this.token = token;
    }

    #region Public

    public event Action Changed;

    public bool HasUnreadMessages { get; private set; }

    public bool HasUnreadMentions { get; private set; }

    public bool IsAway { get; private set; }
    
    public virtual bool Initialize()
    {
      this.Logger.Debug($"Initialize connection using token : {this.token}");

      return this.InitializeConnection();
    }

    public virtual void Dispose()
    {
      this.Changed = null;

      if (this.Client != null)
      {
        this.DeinitializeConnection();
      }
    }

    #endregion

    #region Protected

    protected bool InitializeConnection()
    {
      if (this.ConnectSlackClient())
      {
        this.Logger.Debug("Connection established");

        this.FetchHighlightWords();
        this.FetchInitialMessages();
        this.UpdateStatus();
        this.UpdatePresenceStatus();

        // Bind the Presence change
        this.Client.BindCallback<PresenceChange>(this.OnPresenceChanged);
        this.Client.BindCallback<ManualPresenceChange>(this.OnPresenceChanged);

        // Listen specific messages
        this.Client.BindCallback<ImMarked>(this.OnImMarked);
        this.Client.BindCallback<ChannelMarked>(this.OnChannelMarked);
        this.Client.BindCallback<GroupMarked>(this.OnChannelMarked);
        this.Client.BindCallback<NewMessage>(this.OnMessageReceived);

        return true;
      }
      else
      {
        this.Logger.Error("Unable to connect");

        return false;
      }
    }

    protected void DeinitializeConnection()
    {
      this.Client.UnbindCallback<ImMarked>(this.OnImMarked);
      this.Client.UnbindCallback<ChannelMarked>(this.OnChannelMarked);
      this.Client.UnbindCallback<GroupMarked>(this.OnChannelMarked);
      this.Client.UnbindCallback<NewMessage>(this.OnMessageReceived);

      this.Client.UnbindCallback<PresenceChange>(this.OnPresenceChanged);
      this.Client.UnbindCallback<ManualPresenceChange>(this.OnPresenceChanged);

      this.Client.CloseSocket();
    }

    protected void UpdateStatus()
    {
      foreach (var channelInfo in this.channelsInfo.Where(x => x.Value.UnreadMessage > 0 || x.Value.UnreadMention > 0))
      {
        this.Logger.Debug($"Name: {this.GetReadableName(channelInfo.Key)} - UnreadMessage: {channelInfo.Value.UnreadMessage} - UnreadMention: {channelInfo.Value.UnreadMention}");
      }

      this.HasUnreadMessages = this.channelsInfo.Any(x => x.Value.UnreadMessage > 0);
      this.HasUnreadMentions = this.channelsInfo.Any(x => x.Value.UnreadMention > 0);

      this.Logger.Debug($"HasUnreadMention: {this.HasUnreadMentions}");
      this.Logger.Debug($"HasUnreadMessage: {this.HasUnreadMessages}");

      this.Changed?.Invoke();
    }

    protected void ClearChannelsInfo()
    {
      this.channelsInfo.Clear();
    }

    #endregion

    #region Private

    private bool ConnectSlackClient()
    {
      using (ManualResetEvent connectionEvent = new ManualResetEvent(false))
      using (ManualResetEvent connectionSocketEvent = new ManualResetEvent(false))
      {
        this.Client = new SlackSocketClient(this.token);
        this.Client.RegisterConverter(new JsonRawConverter());

        try
        {
          this.Client.Connect(x => connectionEvent.Set(), () => connectionSocketEvent.Set());

          return WaitHandle.WaitAll(new WaitHandle[] { connectionEvent, connectionSocketEvent }, Timeout);
        }
        catch (Exception ex)
        {
          this.Logger.Debug(ex);

          return false;
        }
        finally
        {
          connectionEvent.Set();
          connectionSocketEvent.Set();
        }
      }
    }

    private void FetchInitialMessages()
    {
      this.Logger.Debug("Fetch initial messages");

      // Retrieve only channels, groups and im visible in Slack client to mimic client behavior
      var selectedChannels = this.Client.Channels.Union(this.Client.Groups).Where(this.ShouldMonitor);
      foreach (var channel in selectedChannels)
      {
        this.UpdateChannelInfo(channel);
      }

      var selectedIms = this.Client.DirectMessages.Where(this.ShouldMonitor);
      foreach (var im in selectedIms)
      {
        this.UpdateChannelInfo(im);
      }
    }

    private void FetchHighlightWords()
    {
      // Add some keywords for mention detection
      this.highlightWords.Clear();
      this.highlightWords.Add("<!channel>");
      this.highlightWords.Add(this.Client.MySelf.id);
      this.highlightWords.Add(this.Client.MySelf.name);
      this.highlightWords.AddRange(this.Client.MySelf.prefs.highlight_words.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
      this.Logger.Debug("Highlight words for mention detection: {0}", string.Join(", ", this.highlightWords));
    }

    private void UpdateChannelInfo(DirectMessageConversation im)
    {
      this.Logger.Debug($"Init IM {this.GetReadableName(im.id)}");

      // Search in unread history
      if (im.unread_count > 0)
      {
        using (ManualResetEventSlim waiter = new ManualResetEventSlim())
        {
          this.Client.GetDirectMessageHistory(
            x =>
            {
              this.channelsInfo[im].UnreadMessage = x.messages.Count(this.IsRegularMessage);
              this.channelsInfo[im].UnreadMention = this.channelsInfo[im].UnreadMessage;
              waiter.Set();
            },
            im, null, null, im.unread_count);
          waiter.Wait(Timeout);
        }
      }
    }

    private void UpdateChannelInfo(Channel channel)
    {
      this.Logger.Debug($"Init {(channel.is_channel ? "channel" : "group")} {this.GetReadableName(channel.id)}");

      // Search in unread history for mentions
      if (channel.unread_count > 0)
      {
        using (ManualResetEventSlim waiter = new ManualResetEventSlim())
        {
          this.GetHistoryMethod(channel)(
            x =>
              {
                this.channelsInfo[channel].UnreadMessage = x.messages.Count(this.IsRegularMessage);
                this.channelsInfo[channel].UnreadMention = x.messages.Count(y => this.IsRegularMessage(y) && this.HasMention(this.GetRawMessage(y)));
                waiter.Set();
              },
            channel, null, null, channel.unread_count);
          waiter.Wait(Timeout);
        }
      }
    }

    private GetHistoryHandler GetHistoryMethod(Channel channel)
    {
      return channel.is_channel
        ? (GetHistoryHandler)this.Client.GetChannelHistory
        : (GetHistoryHandler)this.Client.GetGroupHistory;
    }

    private void OnMessageReceived(NewMessage message)
    {
      if (message.type == "message")
      {
        this.Logger.Debug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {this.GetReadableName(message.channel)} - Raw: {this.GetRawMessage(message)}");

        if (this.IsRegularMessage(message) && this.ShouldMonitor(message.channel))
        {
          this.channelsInfo[message.channel].UnreadMessage++;
          if (this.Client.ChannelLookup.ContainsKey(message.channel) || this.Client.GroupLookup.ContainsKey(message.channel))
          {
            this.channelsInfo[message.channel].UnreadMention += Convert.ToInt32(this.HasMention(this.GetRawMessage(message)));
          }
          else
          {
            this.channelsInfo[message.channel].UnreadMention++;
          }

          this.UpdateStatus();
        }
        else
        {
          this.Logger.Debug("Message dropped");
        }
      }
    }

    private void OnImMarked(ImMarked message)
    {
      this.Logger.Debug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {this.GetReadableName(message.channel)} - Raw: {this.GetRawMessage(message)}");

      if (this.ShouldMonitor(message.channel))
      {
        var directMessageConversation = this.Client.DirectMessageLookup[message.channel];
        var channelNotification = this.channelsInfo[directMessageConversation];

        this.Client.GetDirectMessageHistory(
          x =>
          {
            var messages = x.messages.Where(y => y.ts > message.ts).ToArray();
            channelNotification.UnreadMessage = messages.Count(y => y.user != this.Client.MySelf.id);
            channelNotification.UnreadMention = channelNotification.UnreadMessage;
            this.UpdateStatus();
          },
          directMessageConversation, null, message.ts, HistoryItemsToFetch);
      }
      else
      {
        this.Logger.Debug("Message dropped");
      }
    }

    private void OnChannelMarked(ChannelMarked message)
    {
      this.Logger.Debug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {this.GetReadableName(message.channel)} - Raw: {this.GetRawMessage(message)}");

      if (this.ShouldMonitor(message.channel))
      {
        var channel = message.type == "channel_marked" ? this.Client.ChannelLookup[message.channel] : this.Client.GroupLookup[message.channel];
        var channelNotification = this.channelsInfo[channel];

        this.GetHistoryMethod(channel)(
          x =>
            {
              var messages = x.messages.Where(y => y.ts > message.ts).ToArray();
              channelNotification.UnreadMessage = messages.Count(this.IsRegularMessage);
              channelNotification.UnreadMention = messages.Count(y => this.IsRegularMessage(y) && this.HasMention(this.GetRawMessage(y)));
              this.UpdateStatus();
            },
          channel, null, message.ts, HistoryItemsToFetch);
      }
      else
      {
        this.Logger.Debug("Message dropped");
      }
    }

    private void UpdatePresenceStatus()
    {
      this.Client.GetPresence(userPresence =>
        {
          this.Logger.Debug($"User is currently {userPresence.presence.ToString()}");

          if (userPresence.presence.ToString() == "away")
          {
            this.IsAway = true;
          }
          else
          {
            // Going to assume default of being there.
            this.IsAway = false;
          }

          this.Changed?.Invoke();
        },
        this.Client.MySelf.id);
    }

    private void OnPresenceChanged(PresenceChange message)
    {
      this.UpdatePresenceStatus();
    }

    private string GetReadableName(string channelId)
    {
      var user = this.Client.Users.FirstOrDefault(x => x.id == channelId);
      if (user != null)
      {
        return string.IsNullOrEmpty(user.profile.real_name) ? user.name : user.profile.real_name;
      }

      var im = this.Client.DirectMessages.FirstOrDefault(x => x.id == channelId);
      if (im != null)
      {
        return this.GetReadableName(im.user);
      }

      var channel = this.Client.Channels.Union(this.Client.Groups).FirstOrDefault(x => x.id == channelId);
      if (channel != null)
      {
        return channel.name;
      }

      return "Id not found";
    }

    private bool ShouldMonitor(Channel channel)
    {
      return channel.is_archived == false && ((channel.is_channel && channel.is_member) || channel.is_group);
    }

    private bool ShouldMonitor(DirectMessageConversation im)
    {
      return im.is_user_deleted == false;
    }

    private bool ShouldMonitor(string channelId)
    {
      Channel channel;
      DirectMessageConversation im;
      return (this.Client.ChannelLookup.TryGetValue(channelId, out channel) && this.ShouldMonitor(channel))
             || (this.Client.GroupLookup.TryGetValue(channelId, out channel) && this.ShouldMonitor(channel))
             || (this.Client.DirectMessageLookup.TryGetValue(channelId, out im) && this.ShouldMonitor(im));
    }

    private bool HasMention(string text)
    {
      return text != null && this.highlightWords.Any(x => text.IndexOf(x, StringComparison.InvariantCultureIgnoreCase) != -1);
    }

    private bool IsRegularMessage(Message message)
    {
      return this.IsRegularMessage(message.user, message.subtype);
    }

    private bool IsRegularMessage(NewMessage message)
    {
      return this.IsRegularMessage(message.user, message.subtype);
    }

    private bool IsRegularMessage(string user, string subtype)
    {
      return user != this.Client.MySelf.id && (subtype == null || subtype == "file_share" || subtype == "bot_message");
    }

    private string GetRawMessage(SlackSocketMessage message)
    {
      IRawMessage rawMessage = message as IRawMessage;
      if (rawMessage == null)
      {
        throw new InvalidCastException($"'{message.GetType().FullName}' is not a proxy class and cannot be casted to IRawMessage");
      }
      else
      {
        return rawMessage.Data;
      }
    }

    #endregion
  }
}
