namespace CG.Luxa4Slack
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;

  public class Luxa4Slack
  {
    private const int DelayBeforeUpdate = 1000;

    private readonly object locker = new object();
    private readonly string slackToken;
    private readonly bool showUnreadMentions;
    private readonly bool showUnreadMessages;

    private LuxaforClient luxaforClient;
    private SlackNotificationAgent slackAgent;
    private CancellationTokenSource cancellationTokenSource;

    public Luxa4Slack(string slackToken, bool showUnreadMentions, bool showUnreadMessages)
    {
      if (slackToken == null)
      {
        throw new ArgumentNullException(nameof(slackToken));
      }

      this.slackToken = slackToken;
      this.showUnreadMentions = showUnreadMentions;
      this.showUnreadMessages = showUnreadMessages;
    }

    public event Action LuxaforFailure;

    public void Initialize()
    {
      this.luxaforClient = this.InitializeLuxaforClient();
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

    private LuxaforClient InitializeLuxaforClient()
    {
      var client = new LuxaforClient();
      if (client.Initialize() == false)
      {
        throw new Exception("Luxafor device initialization failed");
      }
      
      if (client.Test())
      {
        return client;
      }
      else
      {
        throw  new Exception("Luxafor communication issue. Please unplug/replug the Luxafor and restart the application");
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
      lock (this.locker)
      {
        this.cancellationTokenSource?.Cancel();
        this.cancellationTokenSource?.Dispose();

        this.cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = this.cancellationTokenSource.Token;

        // Don't execute luxafor update immediately.
        // We wait in case we receive anothe change quickly to avoid blinks
        Task
          .Delay(DelayBeforeUpdate, token)
          .ContinueWith(this.UpdateLuxaforAsync, token);
      }
    }

    private void UpdateLuxaforAsync(Task task)
    {
      bool result;
      if (this.showUnreadMentions && this.slackAgent.HasUnreadMentions)
      {
        result = this.luxaforClient.Set(LuxaforClient.Colors.Red);
      }
      else if (this.showUnreadMessages && this.slackAgent.HasUnreadMessages)
      {
        result = this.luxaforClient.Set(LuxaforClient.Colors.Blue);
      }
      else
      {
        result = this.luxaforClient.Reset();
      }

      if (result == false)
      {
        this.LuxaforFailure?.Invoke();
      }
    }
  }
}
