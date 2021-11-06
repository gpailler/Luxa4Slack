namespace CG.Luxa4Slack
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using CG.Luxa4Slack.Abstractions;
  using CG.Luxa4Slack.Abstractions.Luxafor;

  internal class Luxa4Slack : ILuxa4Slack
  {
    private const int DelayBeforeUpdate = 2000;

    private readonly IEnumerable<string> _slackTokens;
    private readonly bool _showUnreadMentions;
    private readonly bool _showUnreadMessages;
    private readonly bool _showStatus;
    private readonly double _brightness;

    private readonly ILuxaforClient _luxaforClient;
    private readonly ISlackNotificationAgentFactory _slackNotificationAgentFactory;
    private readonly List<ISlackNotificationAgent> _slackAgents;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _updateTask;
    private int _previousWeight;

    public Luxa4Slack(IEnumerable<string> slackTokens, bool showUnreadMentions, bool showUnreadMessages, bool showStatus, double brightness, ILuxaforClient luxaforClient, ISlackNotificationAgentFactory slackNotificationAgentFactory)
    {
      _slackTokens = slackTokens ?? throw new ArgumentNullException(nameof(slackTokens));
      if (!_slackTokens.Any())
      {
        throw new ArgumentException("Empty tokens list", nameof(slackTokens));
      }

      _showUnreadMentions = showUnreadMentions;
      _showUnreadMessages = showUnreadMessages;
      _showStatus = showStatus;
      _brightness = brightness;
      _luxaforClient = luxaforClient;
      _slackNotificationAgentFactory = slackNotificationAgentFactory;
      _slackAgents = new List<ISlackNotificationAgent>();
    }

    public async Task InitializeAsync()
    {
      await InitializeLuxaforClient();

      foreach (var slackAgent in await Task.WhenAll(_slackTokens.Select(InitializeSlackAgentAsync)))
      {
        slackAgent.Changed += OnSlackChanged;
        _slackAgents.Add(slackAgent);
      }

      OnSlackChanged();
    }

    public void Dispose()
    {
      _slackAgents.ForEach(x => x.Dispose());
    }

    private async Task InitializeLuxaforClient()
    {
      _luxaforClient.SetBrightness(_brightness);
      if (!_luxaforClient.IsInitialized)
      {
        throw new Exception("Luxafor device initialization failed");
      }

      await _luxaforClient.StartWaveProcessingAsync();
    }

    private async Task<ISlackNotificationAgent> InitializeSlackAgentAsync(string token)
    {
      var agent = _slackNotificationAgentFactory.Create(token);
      if (await agent.InitializeAsync() == false)
      {
        throw new Exception("Slack connection failed. Please check token is valid");
      }

      return agent;
    }

    private void OnSlackChanged()
    {
      // Cancel previous task if any
      _cancellationTokenSource?.Cancel();
      _cancellationTokenSource?.Dispose();

      // Wait previous task to complete
      if (_updateTask != null)
      {
        Task.WaitAny(_updateTask);
      }

      // Prepare new token
      _cancellationTokenSource = new CancellationTokenSource();
      var token = _cancellationTokenSource.Token;

      // Determine delay before task execution
      // We want to shutdown light immediately but wait in other cases to avoid blinks
      var currentWeight = GetWeight();
      var delay = currentWeight > _previousWeight ? DelayBeforeUpdate : 0;
      _previousWeight = currentWeight;

      _updateTask = Task
          .Delay(delay, token)
          .ContinueWith(UpdateLuxaforAsync, token);
    }

    private int GetWeight()
    {
      var weight = 0;
      weight += _showUnreadMentions && _slackAgents.Any(x => x.HasUnreadMentions) ? 2 : 0;
      weight += _showUnreadMessages && _slackAgents.Any(x => x.HasUnreadMessages) ? 1 : 0;

      return weight;
    }

    private async Task UpdateLuxaforAsync(Task task)
    {
      if (_showUnreadMentions && _slackAgents.Any(x => x.HasUnreadMentions))
      {
        await _luxaforClient.SetAsync(LuxaforColor.Orange);
      }
      else if (_showUnreadMessages && _slackAgents.Any(x => x.HasUnreadMessages))
      {
        await _luxaforClient.SetAsync(LuxaforColor.Blue);
      }
      else if (_showStatus && _slackAgents.Any(x => x.IsAway))
      {
        await _luxaforClient.SetAsync(LuxaforColor.Red);
      }
      else if (_showStatus && _slackAgents.Any(x => x.IsAway == false))
      {
        await _luxaforClient.SetAsync(LuxaforColor.Green);
      }
      else
      {
        await _luxaforClient.ResetAsync();
      }
    }
  }
}
