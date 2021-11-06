namespace CG.Luxa4Slack.Notifications
{
  using System;

  internal class ChannelInfo
  {
    public event Action? Changed;

    public bool HasUnreadMessage { get; private set; }

    public bool HasUnreadMention { get; private set; }

    public void Update(bool hasUnreadMessage, bool hasUnreadMention)
    {
      var changed = false;

      if (HasUnreadMessage != hasUnreadMessage)
      {
        HasUnreadMessage = hasUnreadMessage;
        changed = true;
      }

      if (HasUnreadMention != hasUnreadMention)
      {
        HasUnreadMention = hasUnreadMention;
        changed = true;
      }

      if (changed)
      {
        Changed?.Invoke();
      }
    }
  }
}
