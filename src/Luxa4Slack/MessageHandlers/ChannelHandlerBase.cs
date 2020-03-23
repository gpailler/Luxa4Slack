namespace CG.Luxa4Slack.MessageHandlers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using NLog;
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal abstract class ChannelHandlerBase<TMessage> : MessageHandlerBase
    where TMessage : ChannelMarked
  {
    protected delegate void GetHistoryHandler(Action<MessageHistory> callback, Channel groupInfo, DateTime? latest, DateTime? oldest, int? count, bool? unreads);

    protected ChannelHandlerBase(SlackSocketClient client, HandlerContext context, ILogger logger)
      : base(client, context, logger)
    {
      this.Client.BindCallback<TMessage>(this.OnChannelMarked);

      this.Logger.Debug("Fetch initial messages");
      foreach (var channel in this.GetChannels())
      {
        this.UpdateChannelInfo(channel);
      }
    }

    public override void Dispose()
    {
      this.Client.UnbindCallback<TMessage>(this.OnChannelMarked);

      base.Dispose();
    }

    protected abstract IEnumerable<Channel> GetChannels();

    protected abstract Channel FindChannel(TMessage message);

    protected abstract GetHistoryHandler HistoryMethod { get; }

    private void OnChannelMarked(TMessage message)
    {
      this.Logger.Debug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {this.Context.GetNameFromId(message.channel)} - Raw: {this.GetRawMessage(message)}");

      if (this.ShouldMonitor(message.channel))
      {
        var channel = this.FindChannel(message);
        var channelNotification = this.Context.ChannelsInfo[channel.id];

        this.HistoryMethod(
          x =>
          {
            var messages = x.messages.Where(y => this.FilterMessageByDate(y, message.ts) && this.IsRegularMessage(y));
            channelNotification.Update(
              messages.Any(),
              messages.Any(y => this.HasMention(this.GetRawMessage(y)))
            );
          },
          channel, null, message.ts, HistoryItemsToFetch, false);
      }
      else
      {
        this.Logger.Debug("Message dropped");
      }
    }

    private void UpdateChannelInfo(Channel channel)
    {
      this.Logger.Debug($"Init {(channel.is_channel ? "channel" : "group")} {this.Context.GetNameFromId(channel.id)}");

      int unreadCount = 0;
      using (ManualResetEventSlim waiter = new ManualResetEventSlim())
      {
        this.HistoryMethod(
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
          this.HistoryMethod(
            x =>
            {
              var messages = x.messages.Where(y => this.FilterMessageByDate(y, channel.last_read) && this.IsRegularMessage(y));
              this.Context.ChannelsInfo[channel.id].Update(
                messages.Any(),
                messages.Any(y => this.HasMention(this.GetRawMessage(y)))
              );
              waiter.Set();
            },
            channel, null, null, unreadCount, false);
          waiter.Wait(SlackNotificationAgent.Timeout);
        }
      }
    }
  }
}
