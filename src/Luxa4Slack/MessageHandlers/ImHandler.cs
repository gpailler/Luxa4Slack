namespace CG.Luxa4Slack.MessageHandlers
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using NLog;
  using SlackAPI;

  internal class ImHandler : MessageHandlerBase
  {
    public ImHandler(SlackSocketClient client, HandlerContext context)
      : base(client, context, LogManager.GetLogger(nameof(ImHandler)))
    {
      this.Client.BindCallback<ImMarked>(this.OnImMarked);

      this.Logger.Debug("Fetch initial messages");

      var allChannels = new List<Channel>();
      string cursor = null;
      do
      {
        using (ManualResetEventSlim waiter = new ManualResetEventSlim())
        {
          this.Client.GetConversationsList(response =>
            {
              foreach (var channel in response.channels)
              {
                if (channel.is_im && this.Client.UserLookup.ContainsKey(channel.user))
                {
                  allChannels.Add(channel);
                }
              }

              cursor = response.response_metadata.next_cursor;
              waiter.Set();
            },
            cursor,
            limit: 500,
            types: new[] { "mpim", "im" });

          waiter.Wait();
        }
      } while (!string.IsNullOrEmpty(cursor));

      this.RunParallel(allChannels, this.UpdateChannelInfo);
    }

    public override void Dispose()
    {
      this.Client.UnbindCallback<ImMarked>(this.OnImMarked);

      base.Dispose();
    }

    private void OnImMarked(ImMarked message)
    {
      this.Logger.Debug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {this.Context.GetNameFromId(message.channel)} - Raw: {this.GetRawMessage(message)}");

      if (this.ShouldMonitor(message.channel))
      {
        var directMessageConversation = new Channel() { id = message.channel };
        var channelNotification = this.Context.ChannelsInfo[directMessageConversation.id];

        this.Client.GetConversationsHistory(
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

    private void UpdateChannelInfo(Channel channel)
    {
      this.Logger.Debug($"Init IM {this.Context.GetNameFromId(channel.id)}");

      int unreadCount = 0;
      using (ManualResetEventSlim waiter = new ManualResetEventSlim())
      {
        this.Client.GetConversationsHistory(
          x =>
          {
            unreadCount = x.unread_count_display;
            waiter.Set();
          },
          channel, null, null, 1, true);
        waiter.Wait(SlackNotificationAgent.Timeout);
      }

      if (unreadCount > 0)
      {
        using (ManualResetEventSlim waiter = new ManualResetEventSlim())
        {
          this.Client.GetConversationsHistory(
            x =>
            {
              var hasMessage = x.messages.Any(y => this.FilterMessageByDate(y, channel.last_read) && this.IsRegularMessage(y));
              this.Context.ChannelsInfo[channel.id].Update(hasMessage, hasMessage);
              waiter.Set();
            },
            channel, null, null, unreadCount);
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
