namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;
  using SlackAPI;
  using SlackAPI.RPCMessages;

  internal class ImHandler : MessageHandlerBase
  {
    public ImHandler(SlackSocketClient client, HandlerContext context, ILogger logger)
      : base(client, context, logger)
    {
    }

    public override async Task InitializeAsync()
    {
      Logger.LogDebug("Fetch initial messages");
      Client.BindCallback<ImMarked>(OnImMarked);

      var allChannels = await GetAllChannelsAsync();
      await Parallel.ForEachAsync(allChannels, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, UpdateChannelInfoAsync);
    }

    public override void Dispose()
    {
      Client.UnbindCallback<ImMarked>(OnImMarked);

      base.Dispose();
    }

    private async void OnImMarked(ImMarked message)
    {
      Logger.LogDebug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {Context.GetNameFromId(message.channel)} - Raw: {GetRawMessage(message)}");

      if (ShouldMonitor(message.channel))
      {
        var directMessageConversation = new Channel() { id = message.channel };
        var channelNotification = Context.ChannelsInfo[directMessageConversation.id];
        var messages = await RunSlackClientMethodAsync<ConversationsMessageHistory, Message[]>(
          x => Client.GetConversationsHistory(x, directMessageConversation, null, message.ts, HistoryItemsToFetch),
          x => x.messages);
        messages = messages.Where(y => FilterMessageByDate(y, message.ts)).ToArray();
        var hasUnreadMessages = messages.Any(y => y.user != Client.MySelf.id);
        channelNotification.Update(hasUnreadMessages, hasUnreadMessages);
      }
      else
      {
        Logger.LogDebug("Message dropped");
      }
    }

    private async Task<List<Channel>> GetAllChannelsAsync()
    {
      var allChannels = new List<Channel>();
      string? cursor = null;
      do
      {
        var response = await RunSlackClientMethodAsync<ConversationsListResponse, (Channel[] channels, string nextCursor)>(
            x => Client.GetConversationsList(x, cursor, limit: 500, types: new[] { "mpim", "im" }),
            x => (x.channels, x.response_metadata.next_cursor));

        allChannels.AddRange(response.channels.Where(x => x.is_im && Client.UserLookup.ContainsKey(x.user)));
        cursor = response.nextCursor;
      } while (!string.IsNullOrEmpty(cursor));

      return allChannels;
    }

    private async ValueTask UpdateChannelInfoAsync(Channel channel, CancellationToken _)
    {
      Logger.LogDebug($"Init IM {Context.GetNameFromId(channel.id)}");

      var unreadCount = await RunSlackClientMethodAsync<ConversationsMessageHistory, int>(
        x => Client.GetConversationsHistory(x, channel, null, null, 1, true),
          x => x.unread_count_display);
      if (unreadCount > 0)
      {
        var messages = await RunSlackClientMethodAsync<ConversationsMessageHistory, Message[]>(
          x => Client.GetConversationsHistory(x, channel, null, null, unreadCount),
          x => x.messages);

        var hasMessage = messages.Any(y => FilterMessageByDate(y, channel.last_read) && IsRegularMessage(y));
        Context.ChannelsInfo[channel.id].Update(hasMessage, hasMessage);
      }
    }

    protected override bool ShouldMonitor(string id)
    {
      return Client.DirectMessageLookup.TryGetValue(id, out var im) && im.is_user_deleted == false;
    }
  }
}
