namespace CG.Luxa4Slack
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using CG.Luxa4Slack.Extensions;
  using CG.Luxa4Slack.MessageHandlers;
  using NLog;

  using SlackAPI;

  internal class SlackNotificationAgent : IDisposable
  {
    internal const int Timeout = 15000;

    protected readonly ILogger Logger = LogManager.GetLogger(nameof(SlackNotificationAgent));
    protected SlackSocketClient Client;

    private readonly string token;
    private readonly ChannelsInfo channelsInfo = new ChannelsInfo();

    private readonly List<IDisposable> messageHandlers = new List<IDisposable>();
    private ReadableNameResolver readableNameResolver;

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

        this.readableNameResolver = new ReadableNameResolver(this.Client);

        var highlightWords = this.GetHighlightWords();
        this.Logger.Debug("Highlight words for mention detection: {0}", string.Join(", ", highlightWords));

        messageHandlers.Clear();
        messageHandlers.Add(new ChannelHandler(this.Client, this.channelsInfo, highlightWords, this.readableNameResolver));
        messageHandlers.Add(new GroupHandler(this.Client, this.channelsInfo, highlightWords, this.readableNameResolver));
        messageHandlers.Add(new ImHandler(this.Client, this.channelsInfo, highlightWords, this.readableNameResolver));
        messageHandlers.Add(new PresenceHandler(this.Client, this.OnPresenceChanged));
        this.channelsInfo.Changed += this.UpdateStatus;
        this.UpdateStatus();

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
      this.channelsInfo.Changed -= this.UpdateStatus;
      messageHandlers.ForEach(x => x.Dispose());

      this.Client.CloseSocket();
    }

    protected void UpdateStatus()
    {
      foreach (var channelInfo in this.channelsInfo.Where(x => x.Value.HasUnreadMessage || x.Value.HasUnreadMention))
      {
        this.Logger.Debug($"Name: {this.readableNameResolver.Resolve(channelInfo.Key)} - HasUnreadMessage: {channelInfo.Value.HasUnreadMessage} - HasUnreadMention: {channelInfo.Value.HasUnreadMention}");
      }

      this.HasUnreadMessages = this.channelsInfo.Any(x => x.Value.HasUnreadMessage);
      this.HasUnreadMentions = this.channelsInfo.Any(x => x.Value.HasUnreadMention);

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

    private HashSet<string> GetHighlightWords()
    {
      // Add some keywords for mention detection
      var highlightWords = new HashSet<string>();
      highlightWords.Add("<!channel>");
      highlightWords.Add(this.Client.MySelf.id);
      highlightWords.Add(this.Client.MySelf.name);
      this.Client.MySelf.prefs.highlight_words
        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .ForEach(x => highlightWords.Add(x));

      return highlightWords;
    }

    private void OnPresenceChanged(bool isActive)
    {
      this.IsAway = !isActive;
      this.Changed?.Invoke();
    }

    #endregion
  }
}
