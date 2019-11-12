﻿namespace CG.Luxa4Slack
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

  [SlackSocketRouting("manual_presence_change")]
  public class ManualPresenceChange : PresenceChange
  {
  }

  [SlackSocketRouting("presence_sub")]
  public class PresenceSub : SlackSocketMessage
  {
    public PresenceSub(params string[] ids)
    {
      this.ids = ids;
    }

    public string[] ids { get; }
  }
}

