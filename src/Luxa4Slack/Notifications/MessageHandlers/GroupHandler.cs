namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System.Collections.Generic;
  using System.Linq;
  using Microsoft.Extensions.Logging;
  using SlackAPI;

  internal class GroupHandler : ChannelHandlerBase<GroupMarked>
  {
    public GroupHandler(SlackSocketClient client, HandlerContext context, ILogger logger)
      : base(client, context, logger)
    {
    }

    protected override IEnumerable<Channel> GetChannels()
    {
      return Client.Groups.Where(x => ShouldMonitor(x.id));
    }

    protected override Channel FindChannel(GroupMarked message)
    {
      return Client.GroupLookup[message.channel];
    }

    protected override GetHistoryHandler HistoryMethod => Client.GetConversationsHistory;

    protected override bool ShouldMonitor(string id)
    {
      return Client.GroupLookup.TryGetValue(id, out var channel) && !channel.is_archived;
    }
  }
}
