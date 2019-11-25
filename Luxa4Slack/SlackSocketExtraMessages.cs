namespace CG.Luxa4Slack
{
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  [SlackSocketRouting("im_marked")]
  public class ImMarked : ChannelMarked
  {
  }

  [SlackSocketRouting("group_marked")]
  public class GroupMarked : ChannelMarked
  {
  }
}
