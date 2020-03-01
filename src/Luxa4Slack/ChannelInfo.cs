namespace CG.Luxa4Slack
{
  using System;

  internal class ChannelInfo
  {
    public event Action Changed;

    public bool HasUnreadMessage { get; private set; }

    public bool HasUnreadMention { get; private set; }

    public void Update(bool hasUnreadMessage, bool hasUnreadMention)
    {
      bool changed = false;

      if (this.HasUnreadMessage != hasUnreadMessage)
      {
        this.HasUnreadMessage = hasUnreadMessage;
        changed = true;
      }

      if (this.HasUnreadMention != hasUnreadMention)
      {
        this.HasUnreadMention = hasUnreadMention;
        changed = true;
      }

      if (changed)
      {
        this.Changed?.Invoke();
      }
    }
  }
}
