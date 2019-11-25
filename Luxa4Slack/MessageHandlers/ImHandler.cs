namespace CG.Luxa4Slack.MessageHandlers
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using NLog;
  using SlackAPI;

  internal class ImHandler : MessageHandlerBase
  {
    public ImHandler(SlackSocketClient client, ChannelsInfo channelsInfo, HashSet<string> highlightWords, ReadableNameResolver readableNameResolver)
      : base(client, channelsInfo, highlightWords, readableNameResolver, LogManager.GetLogger(nameof(ImHandler)))
    {
      this.Client.BindCallback<ImMarked>(this.OnImMarked);

      this.Logger.Debug("Fetch initial messages");
      foreach (var channel in this.Client.DirectMessages.Where(x => this.ShouldMonitor(x.id)))

      {
        this.UpdateChannelInfo(channel);
      }
    }

    public override void Dispose()
    {
      this.Client.UnbindCallback<ImMarked>(this.OnImMarked);

      base.Dispose();
    }

    private void OnImMarked(ImMarked message)
    {
      this.Logger.Debug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {this.ReadableNameResolver.Resolve(message.channel)} - Raw: {this.GetRawMessage(message)}");

      if (this.ShouldMonitor(message.channel))
      {
        var directMessageConversation = this.Client.DirectMessageLookup[message.channel];
        var channelNotification = this.ChannelsInfo[directMessageConversation.id];

        this.Client.GetDirectMessageHistory(
          x =>
          {
            var messages = x.messages.Where(y => this.FilterMessageByDate(y, message.ts));
            var hasUnreadMessages = messages.Any(y => y.user != this.Client.MySelf.id);
            channelNotification.Update(hasUnreadMessages, hasUnreadMessages);
          },
          directMessageConversation, null, message.ts, HistoryItemsToFetch);
      }
      else
      {
        this.Logger.Debug("Message dropped");
      }
    }

    private void UpdateChannelInfo(DirectMessageConversation im)
    {
      this.Logger.Debug($"Init IM {this.ReadableNameResolver.Resolve(im.id)}");

      int unreadCount = 0;
      using (ManualResetEventSlim waiter = new ManualResetEventSlim())
      {
        this.Client.GetDirectMessageHistory(
          x =>
          {
            unreadCount = x.unread_count_display;
            waiter.Set();
          },
          im, null, null, 1, true);
        waiter.Wait(SlackNotificationAgent.Timeout);
      }

      if (unreadCount > 0)
      {
        using (ManualResetEventSlim waiter = new ManualResetEventSlim())
        {
          this.Client.GetDirectMessageHistory(
            x =>
            {
              var hasMessage = x.messages.Any(y => this.FilterMessageByDate(y, im.last_read) && this.IsRegularMessage(y));
              this.ChannelsInfo[im.id].Update(hasMessage, hasMessage);
              waiter.Set();
            },
            im, null, null, unreadCount);
          waiter.Wait(SlackNotificationAgent.Timeout);
        }
      }
    }

    protected override bool ShouldMonitor(string id)
    {
      return this.Client.DirectMessageLookup.TryGetValue(id, out var im) && im.is_user_deleted == false;
    }
  }
}
