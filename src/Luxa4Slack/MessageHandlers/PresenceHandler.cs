namespace CG.Luxa4Slack.MessageHandlers
{
  using System;
  using NLog;
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal class PresenceHandler : IDisposable
  {
    private readonly ILogger logger = LogManager.GetLogger(nameof(PresenceHandler));

    private readonly SlackSocketClient client;
    private readonly Action<bool> onPresenceChanged;

    public PresenceHandler(SlackSocketClient client, Action<bool> onPresenceChanged)
    {
      this.client = client;
      this.onPresenceChanged = onPresenceChanged;

      // Bind the Presence change
      this.client.OnPresenceChanged += this.OnPresenceChanged;
      this.client.SubscribePresenceChange(this.client.MySelf.id);
    }

    public void Dispose()
    {
      this.client.OnPresenceChanged -= this.OnPresenceChanged;
    }

    private void OnPresenceChanged(PresenceChange message)
    {
      this.logger.Debug($"User is currently {message.presence.ToString()}");
      this.onPresenceChanged(message.presence != Presence.away);
    }
  }
}
