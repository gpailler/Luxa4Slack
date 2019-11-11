namespace CG.Luxa4Slack
{
  using System;

  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  [SlackSocketRouting("im_marked")]
  public class ImMarked : SlackSocketMessage
  {
    public string channel;
    public DateTime ts;
  }

  [SlackSocketRouting("group_marked")]
  public class GroupMarked : ChannelMarked
  {
  }

  /*
  [SlackSocketRouting("manual_presence_change", null)]
  public class ManualPresenceChange : SlackSocketMessage
  {
    public string user;
    public Presence presence;

  }
  */

}

namespace SlackAPI.WebSocketMessages
{
  [SlackSocketRouting("manual_presence_change", null)]
  public class ManualPresenceChange : SlackSocketMessage
  {
    public string user;
    public Presence presence;

  }
}

