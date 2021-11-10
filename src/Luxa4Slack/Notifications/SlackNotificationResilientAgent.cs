namespace CG.Luxa4Slack.Notifications
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using CG.Luxa4Slack.Abstractions;
  using Microsoft.Extensions.Logging;
  using SlackAPI;

  internal class SlackNotificationResilientAgent : ISlackNotificationAgent
  {
    private const int WatchDogDelay = 10000;

    private readonly string _token;
    private readonly Func<string, SlackSocketClient> _slackSocketClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SlackNotificationResilientAgent> _logger;
    private readonly CancellationTokenSource _watchDogToken = new();
    private readonly ManualResetEventSlim _waitHandle = new(false);

    private ISlackNotificationAgent? _slackNotificationAgent;
    private SlackSocketClient? _slackSocketClient;

    public SlackNotificationResilientAgent(string token, Func<string, SlackSocketClient> slackSocketClientFactory, ILoggerFactory loggerFactory)
    {
      _token = token;
      _slackSocketClientFactory = slackSocketClientFactory;
      _loggerFactory = loggerFactory;
      _logger = loggerFactory.CreateLogger<SlackNotificationResilientAgent>();
    }

    public event Action? Changed;

    public bool HasUnreadMessages
    {
      get
      {
        _waitHandle.Wait();
        return _slackNotificationAgent?.HasUnreadMessages ?? false;
      }
    }

    public bool HasUnreadMentions
    {
      get
      {
        _waitHandle.Wait();
        return _slackNotificationAgent?.HasUnreadMentions ?? false;
      }
    }

    public bool IsAway
    {
      get
      {
        _waitHandle.Wait();
        return _slackNotificationAgent?.IsAway ?? false;
      }
    }

    public async Task<bool> InitializeAsync()
    {
      var result = await InitializeDecorateeAsync();
      if (result)
      {
#pragma warning disable 4014
        Task.Factory.StartNew(WatchDog, _watchDogToken);
#pragma warning restore 4014
      }

      return result;
    }

    public void Dispose()
    {
      _watchDogToken.Cancel();
      _slackNotificationAgent?.Dispose();
    }

    private async Task<bool> InitializeDecorateeAsync()
    {
      _slackSocketClient = _slackSocketClientFactory(_token);
      _slackNotificationAgent = new SlackNotificationAgentFactory(_ => _slackSocketClient, _loggerFactory).Create(_token);
      var result = await _slackNotificationAgent.InitializeAsync();

      if (result)
      {
        _slackNotificationAgent.Changed += OnChanged;
        _waitHandle.Set();
      }

      return result;
    }

    private void OnChanged()
    {
      Changed?.Invoke();
    }

    private async Task WatchDog(object _)
    {
      while (_watchDogToken.IsCancellationRequested == false)
      {
        TrySendPing();
        if (_slackSocketClient?.IsConnected == false || _slackNotificationAgent == null)
        {
          _logger.LogWarning("Slack connection down. Reconnecting");

          if (_slackNotificationAgent != null)
          {
            _waitHandle.Reset();
            _slackNotificationAgent.Changed -= OnChanged;
            _slackNotificationAgent.Dispose();
          }

          await InitializeDecorateeAsync();
        }

        Thread.Sleep(WatchDogDelay);
      }
    }

    private void TrySendPing()
    {
      try
      {
        _slackSocketClient?.SendPing();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "SendPing failed");
      }
    }
  }
}
