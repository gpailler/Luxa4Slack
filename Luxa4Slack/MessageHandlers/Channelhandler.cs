namespace CG.Luxa4Slack.MessageHandlers
{
  using System.Collections.Generic;
  using System.Linq;
  using NLog;
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal class ChannelHandler : ChannelHandlerBase<ChannelMarked>
  {
    public ChannelHandler(SlackSocketClient client, ChannelsInfo channelsInfo, HashSet<string> highlightWords, ReadableNameResolver readableNameResolver)
      : base(client, channelsInfo, highlightWords, readableNameResolver, LogManager.GetLogger(nameof(ChannelHandler)))
    {
    }

    protected override IEnumerable<Channel> GetChannels()
    {
      return this.Client.Channels.Where(x => this.ShouldMonitor(x.id));
    }

    protected override Channel FindChannel(ChannelMarked message)
    {
      return this.Client.ChannelLookup[message.channel];
    }

    protected override GetHistoryHandler HistoryMethod => this.Client.GetChannelHistory;

    protected override bool ShouldMonitor(string id)
    {
      return this.Client.ChannelLookup.TryGetValue(id, out var channel) && !channel.is_archived && channel.is_member;
    }
  }
}
