namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using Microsoft.Extensions.Logging;
  using SlackAPI;

  internal class ImHandler : MessageHandlerBase
  {
    public ImHandler(SlackSocketClient client, HandlerContext context, ILogger logger)
      : base(client, context, logger)
    {
      Client.BindCallback<ImMarked>(OnImMarked);

      Logger.LogDebug("Fetch initial messages");

      var allChannels = new List<Channel>();
      string? cursor = null;
      do
      {
        using var waiter = new ManualResetEventSlim();
        Client.GetConversationsList(response =>
          {
            foreach (var channel in response.channels)
            {
              if (channel.is_im && Client.UserLookup.ContainsKey(channel.user))
              {
                allChannels.Add(channel);
              }
            }

            cursor = response.response_metadata.next_cursor;
            // ReSharper disable once AccessToDisposedClosure
            waiter.Set();
          },
          cursor,
          limit: 500,
          types: new[] { "mpim", "im" });

        waiter.Wait();
      } while (!string.IsNullOrEmpty(cursor));

      RunParallel(allChannels, UpdateChannelInfo);
    }

    public override void Dispose()
    {
      Client.UnbindCallback<ImMarked>(OnImMarked);

      base.Dispose();
    }

    private void OnImMarked(ImMarked message)
    {
      Logger.LogDebug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {Context.GetNameFromId(message.channel)} - Raw: {GetRawMessage(message)}");

      if (ShouldMonitor(message.channel))
      {
        var directMessageConversation = new Channel() { id = message.channel };
        var channelNotification = Context.ChannelsInfo[directMessageConversation.id];

        Client.GetConversationsHistory(
          x =>
          {
            var messages = x.messages.Where(y => FilterMessageByDate(y, message.ts));
            var hasUnreadMessages = messages.Any(y => y.user != Client.MySelf.id);
            channelNotification.Update(hasUnreadMessages, hasUnreadMessages);
          },
          directMessageConversation, null, message.ts, HistoryItemsToFetch);
      }
      else
      {
        Logger.LogDebug("Message dropped");
      }
    }

    private void UpdateChannelInfo(Channel channel)
    {
      Logger.LogDebug($"Init IM {Context.GetNameFromId(channel.id)}");

      var unreadCount = 0;
      using (var waiter = new ManualResetEventSlim())
      {
        Client.GetConversationsHistory(
          x =>
          {
            unreadCount = x.unread_count_display;
            // ReSharper disable once AccessToDisposedClosure
            waiter.Set();
          },
          channel, null, null, 1, true);
        waiter.Wait(SlackNotificationAgent.Timeout);
      }

      if (unreadCount > 0)
      {
        using var waiter = new ManualResetEventSlim();
        Client.GetConversationsHistory(
          x =>
          {
            var hasMessage = x.messages.Any(y => FilterMessageByDate(y, channel.last_read) && IsRegularMessage(y));
            Context.ChannelsInfo[channel.id].Update(hasMessage, hasMessage);
            // ReSharper disable once AccessToDisposedClosure
            waiter.Set();
          },
          channel, null, null, unreadCount);
        waiter.Wait(SlackNotificationAgent.Timeout);
      }
    }

    protected override bool ShouldMonitor(string id)
    {
      return Client.DirectMessageLookup.TryGetValue(id, out var im) && im.is_user_deleted == false;
    }
  }
}
