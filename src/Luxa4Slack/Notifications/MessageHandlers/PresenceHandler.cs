namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System;
  using System.Threading.Tasks;
  using CG.Luxa4Slack.Abstractions;
  using Microsoft.Extensions.Logging;
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal class PresenceHandler : IMessageHandler
  {
    private readonly SlackSocketClient _client;
    private readonly Action<bool> _onPresenceChanged;
    private readonly ILogger _logger;

    public PresenceHandler(SlackSocketClient client, Action<bool> onPresenceChanged, ILogger logger)
    {
      _client = client;
      _onPresenceChanged = onPresenceChanged;
      _logger = logger;
    }

    public Task InitializeAsync()
    {
      _client.OnPresenceChanged += OnPresenceChanged;
      _client.SubscribePresenceChange(_client.MySelf.id);

      return Task.CompletedTask;
    }

    public void Dispose()
    {
      _client.OnPresenceChanged -= OnPresenceChanged;
    }

    private void OnPresenceChanged(PresenceChange message)
    {
      _logger.LogDebug($"User is currently {message.presence}");
      _onPresenceChanged(message.presence != Presence.away);
    }
  }
}
