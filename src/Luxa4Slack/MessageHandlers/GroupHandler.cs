namespace CG.Luxa4Slack.MessageHandlers
{
  using System.Collections.Generic;
  using System.Linq;
  using NLog;
  using SlackAPI;

  internal class GroupHandler : ChannelHandlerBase<GroupMarked>
  {
    public GroupHandler(SlackSocketClient client, HandlerContext context)
      : base(client, context, LogManager.GetLogger(nameof(GroupHandler)))
    {
    }

    protected override IEnumerable<Channel> GetChannels()
    {
      return this.Client.Groups.Where(x => this.ShouldMonitor(x.id));
    }

    protected override Channel FindChannel(GroupMarked message)
    {
      return this.Client.GroupLookup[message.channel];
    }

    protected override GetHistoryHandler HistoryMethod => this.Client.GetConversationsHistory;

    protected override bool ShouldMonitor(string id)
    {
      return this.Client.GroupLookup.TryGetValue(id, out var channel) && !channel.is_archived;
    }
  }
}
