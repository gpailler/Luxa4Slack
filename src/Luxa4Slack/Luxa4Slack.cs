﻿namespace CG.Luxa4Slack
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;

  public class Luxa4Slack
  {
    private const int DelayBeforeUpdate = 2000;

    private readonly IEnumerable<string> slackTokens;
    private readonly bool showUnreadMentions;
    private readonly bool showUnreadMessages;
    private readonly bool showStatus;
    private readonly double brightness;

    private LuxaforClient luxaforClient;
    private List<SlackNotificationAgent> slackAgents;
    private CancellationTokenSource cancellationTokenSource;
    private Task updateTask;
    private int previousWeight;

    public Luxa4Slack(IEnumerable<string> slackTokens, bool showUnreadMentions, bool showUnreadMessages, bool showStatus, double brightness)
    {
      this.slackTokens = slackTokens ?? throw new ArgumentNullException(nameof(slackTokens));
      if (!this.slackTokens.Any())
      {
        throw new ArgumentException("Empty tokens list", nameof(slackTokens));
      }

      this.showUnreadMentions = showUnreadMentions;
      this.showUnreadMessages = showUnreadMessages;
      this.showStatus = showStatus;
      this.brightness = brightness;
      this.slackAgents = new List<SlackNotificationAgent>();
    }

    public event Action LuxaforFailure;

    public async Task Initialize()
    {
      this.luxaforClient = await this.InitializeLuxaforClient();
      await this.luxaforClient.StartWaveProcessingAsync();

      foreach (var slackToken in this.slackTokens)
      {
        var slackAgent = this.InitializeSlackAgent(slackToken);
        slackAgent.Changed += this.OnSlackChanged;
        this.slackAgents.Add(slackAgent);
      }

      this.OnSlackChanged();
    }

    public void Dispose()
    {
      this.LuxaforFailure = null;

      this.slackAgents?.ForEach(x => x.Dispose());
      this.luxaforClient?.Dispose();
    }

    private async Task<LuxaforClient> InitializeLuxaforClient()
    {
      var client = new LuxaforClient(this.brightness);
      if (client.Initialize() == false)
      {
        throw new Exception("Luxafor device initialization failed");
      }

      if (await client.TestAsync())
      {
        return client;
      }
      else
      {
        throw new Exception("Luxafor communication issue. Please unplug/replug the Luxafor and restart the application");
      }
    }

    private SlackNotificationAgent InitializeSlackAgent(string token)
    {
      var agent = new SlackNotificationAgentReconnectable(token);
      if (agent.Initialize() == false)
      {
        throw new Exception("Slack connection failed. Please check token is valid");
      }

      return agent;
    }

    private void OnSlackChanged()
    {
      // Cancel previous task if any
      this.cancellationTokenSource?.Cancel();
      this.cancellationTokenSource?.Dispose();

      // Wait previous task to complete
      if (this.updateTask != null)
      {
        Task.WaitAny(this.updateTask);
      }

      // Prepare new token
      this.cancellationTokenSource = new CancellationTokenSource();
      CancellationToken token = this.cancellationTokenSource.Token;

      // Determine delay before task execution
      // We want to shutdown light immediately but wait in other cases to avoid blinks
      int currentWeight = this.GetWeight();
      int delay = currentWeight > this.previousWeight ? DelayBeforeUpdate : 0;
      this.previousWeight = currentWeight;

      this.updateTask = Task
          .Delay(delay, token)
          .ContinueWith(this.UpdateLuxaforAsync, token);
    }

    private int GetWeight()
    {
      int weight = 0;
      weight += this.showUnreadMentions && this.slackAgents.Any(x => x.HasUnreadMentions) ? 2 : 0;
      weight += this.showUnreadMessages && this.slackAgents.Any(x => x.HasUnreadMessages) ? 1 : 0;

      return weight;
    }

    private async Task UpdateLuxaforAsync(Task task)
    {
      bool result;
      if (this.showUnreadMentions && this.slackAgents.Any(x => x.HasUnreadMentions))
      {
        result = await this.luxaforClient.SetAsync(LuxaforClient.Colors.Orange);
      }
      else if (this.showUnreadMessages && this.slackAgents.Any(x => x.HasUnreadMessages))
      {
        result = await this.luxaforClient.SetAsync(LuxaforClient.Colors.Blue);
      }
      else if (this.showStatus && this.slackAgents.Any(x => x.IsAway))
      {
        result = await this.luxaforClient.SetAsync(LuxaforClient.Colors.Red);
      }
      else if (this.showStatus && this.slackAgents.Any(x => x.IsAway == false))
      {
        result = await this.luxaforClient.SetAsync(LuxaforClient.Colors.Green);
      }
      else
      {
        result = await this.luxaforClient.ResetAsync();
      }

      if (result == false)
      {
        this.LuxaforFailure?.Invoke();
      }
    }
  }
}
