namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using Microsoft.Extensions.Logging;
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal abstract class ChannelHandlerBase<TMessage> : MessageHandlerBase
    where TMessage : ChannelMarked
  {
    protected delegate void GetHistoryHandler(Action<MessageHistory> callback, Channel groupInfo, DateTime? latest, DateTime? oldest, int? count, bool? unreads);

    protected ChannelHandlerBase(SlackSocketClient client, HandlerContext context, ILogger logger)
      : base(client, context, logger)
    {
      Client.BindCallback<TMessage>(OnChannelMarked);

      Logger.LogDebug("Fetch initial messages");

      RunParallel(GetChannels(), UpdateChannelInfo);
    }

    public override void Dispose()
    {
      Client.UnbindCallback<TMessage>(OnChannelMarked);

      base.Dispose();
    }

    protected abstract IEnumerable<Channel> GetChannels();

    protected abstract Channel FindChannel(TMessage message);

    protected abstract GetHistoryHandler HistoryMethod { get; }

    private void OnChannelMarked(TMessage message)
    {
      Logger.LogDebug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {Context.GetNameFromId(message.channel)} - Raw: {GetRawMessage(message)}");

      if (ShouldMonitor(message.channel))
      {
        var channel = FindChannel(message);
        var channelNotification = Context.ChannelsInfo[channel.id];

        HistoryMethod(
          x =>
          {
            var messages = x.messages.Where(y => FilterMessageByDate(y, message.ts) && IsRegularMessage(y)).ToArray();
            channelNotification.Update(
              messages.Any(),
              messages.Any(y => HasMention(GetRawMessage(y)))
            );
          },
          channel, null, message.ts, HistoryItemsToFetch, false);
      }
      else
      {
        Logger.LogDebug("Message dropped");
      }
    }

    private void UpdateChannelInfo(Channel channel)
    {
      Logger.LogDebug($"Init {(channel.is_channel ? "channel" : "group")} {Context.GetNameFromId(channel.id)}");

      var unreadCount = 0;
      using (var waiter = new ManualResetEventSlim())
      {
        HistoryMethod(
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
        HistoryMethod(
          x =>
          {
            var messages = x.messages.Where(y => FilterMessageByDate(y, channel.last_read) && IsRegularMessage(y)).ToArray();
            Context.ChannelsInfo[channel.id].Update(
              messages.Any(),
              messages.Any(y => HasMention(GetRawMessage(y)))
            );
            // ReSharper disable once AccessToDisposedClosure
            waiter.Set();
          },
          channel, null, null, unreadCount, false);
        waiter.Wait(SlackNotificationAgent.Timeout);
      }
    }
  }
}
