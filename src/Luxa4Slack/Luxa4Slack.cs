namespace CG.Luxa4Slack
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;

  public class Luxa4Slack
  {
    private const int DelayBeforeUpdate = 2000;
    
    private readonly string slackToken;
    private readonly bool showUnreadMentions;
    private readonly bool showUnreadMessages;
    private readonly bool showStatus;
    private readonly double brightness;

    private LuxaforClient luxaforClient;
    private SlackNotificationAgent slackAgent;
    private CancellationTokenSource cancellationTokenSource;
    private Task updateTask;
    private int previousWeight;

    public Luxa4Slack(string slackToken, bool showUnreadMentions, bool showUnreadMessages, bool showStatus, double brightness)
    {
      if (slackToken == null)
      {
        throw new ArgumentNullException(nameof(slackToken));
      }

      this.slackToken = slackToken;
      this.showUnreadMentions = showUnreadMentions;
      this.showUnreadMessages = showUnreadMessages;
      this.showStatus = showStatus;
      this.brightness = brightness;
    }

    public event Action LuxaforFailure;

    public async Task Initialize()
    {
      this.luxaforClient = await this.InitializeLuxaforClient();
      await this.luxaforClient.StartWaveProcessingAsync();

      this.slackAgent = this.InitializeSlackAgent(this.slackToken);

      this.slackAgent.Changed += this.OnSlackChanged;
      this.OnSlackChanged();
    }

    public void Dispose()
    {
      this.LuxaforFailure = null;

      this.slackAgent?.Dispose();
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
      weight += this.showUnreadMentions && this.slackAgent.HasUnreadMentions ? 2 : 0;
      weight += this.showUnreadMessages && this.slackAgent.HasUnreadMessages ? 1 : 0;

      return weight;
    }

    private async Task UpdateLuxaforAsync(Task task)
    {
      bool result;
      if (this.showUnreadMentions && this.slackAgent.HasUnreadMentions)
      {
        result = await this.luxaforClient.SetAsync(LuxaforClient.Colors.Orange);
      }
      else if (this.showUnreadMessages && this.slackAgent.HasUnreadMessages)
      {
        result = await this.luxaforClient.SetAsync(LuxaforClient.Colors.Blue);
      }
      else if (this.showStatus && this.slackAgent.IsAway)
      {
        result = await this.luxaforClient.SetAsync(LuxaforClient.Colors.Red);
      }
      else if (this.showStatus && this.slackAgent.IsAway == false)
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
