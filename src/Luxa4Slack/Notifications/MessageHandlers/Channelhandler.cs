namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System.Collections.Generic;
  using System.Linq;
  using Microsoft.Extensions.Logging;
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal class ChannelHandler : ChannelHandlerBase<ChannelMarked>
  {
    public ChannelHandler(SlackSocketClient client, HandlerContext context, ILogger logger)
      : base(client, context, logger)
    {
    }

    protected override IEnumerable<Channel> GetChannels()
    {
      return Client.Channels.Where(x => ShouldMonitor(x.id));
    }

    protected override Channel FindChannel(ChannelMarked message)
    {
      return Client.ChannelLookup[message.channel];
    }

    protected override GetHistoryHandler HistoryMethod => Client.GetConversationsHistory;

    protected override bool ShouldMonitor(string id)
    {
      return Client.ChannelLookup.TryGetValue(id, out var channel) && !channel.is_archived && channel.is_member;
    }
  }
}
