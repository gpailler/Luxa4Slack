namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using Dasync.Collections;
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
    }

    public override async Task InitializeAsync()
    {
      Logger.LogDebug("Fetch initial messages");

      Client.BindCallback<TMessage>(OnChannelMarked);
      await GetChannels().ParallelForEachAsync(UpdateChannelInfoAsync, MaxDegreeOfParallelism);
    }

    public override void Dispose()
    {
      Client.UnbindCallback<TMessage>(OnChannelMarked);

      base.Dispose();
    }

    protected abstract IEnumerable<Channel> GetChannels();

    protected abstract Channel FindChannel(TMessage message);

    protected abstract GetHistoryHandler HistoryMethod { get; }

    private async void OnChannelMarked(TMessage message)
    {
      Logger.LogDebug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {Context.GetNameFromId(message.channel)} - Raw: {GetRawMessage(message)}");

      if (ShouldMonitor(message.channel))
      {
        var channel = FindChannel(message);
        var channelNotification = Context.ChannelsInfo[channel.id];

        var messages = await RunSlackClientMethodAsync<MessageHistory, Message[]>(
          x => HistoryMethod(x, channel, null, message.ts, HistoryItemsToFetch, false),
          x => x.messages);

        messages = messages.Where(y => FilterMessageByDate(y, message.ts) && IsRegularMessage(y)).ToArray();
        channelNotification.Update(
          messages.Any(),
          messages.Any(y => HasMention(GetRawMessage(y)))
        );
      }
      else
      {
        Logger.LogDebug("Message dropped");
      }
    }

    private async Task UpdateChannelInfoAsync(Channel channel)
    {
      Logger.LogDebug($"Init {(channel.is_channel ? "channel" : "group")} {Context.GetNameFromId(channel.id)}");

      var unreadCount = await RunSlackClientMethodAsync<MessageHistory, int>(
        x => HistoryMethod(x, channel, null, null, 1, true),
        x => x.unread_count_display);
      if (unreadCount > 0)
      {
        var messages = await RunSlackClientMethodAsync<MessageHistory, Message[]>(
          x => HistoryMethod(x, channel, null, null, unreadCount, false),
          x => x.messages);

        messages = messages.Where(y => FilterMessageByDate(y, channel.last_read) && IsRegularMessage(y)).ToArray();
        Context.ChannelsInfo[channel.id].Update(
          messages.Any(),
          messages.Any(y => HasMention(GetRawMessage(y)))
        );
      }
    }
  }
}
