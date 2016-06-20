namespace CG.Luxa4Slack
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;

  internal class SlackNotificationAgentReconnectable : SlackNotificationAgent
  {
    private const int WatchDogDelay = 10000;

    private readonly CancellationTokenSource watchDogToken = new CancellationTokenSource();

    public SlackNotificationAgentReconnectable(string token)
      : base(token)
    {
    }

    public override bool Initialize()
    {
      bool result = base.Initialize();

      if (result)
      {
        Task.Factory.StartNew(this.WatchDog, this.watchDogToken);
      }

      return result;
    }

    public override void Dispose()
    {
      this.watchDogToken.Cancel();
      base.Dispose();
    }
    
    private void TrySendPing()
    {
      try
      {
        this.Client.SendPing();
      }
      catch (Exception ex)
      {
        this.Logger.Error(ex, "SendPing failed");
      }
    }

    private void WatchDog(object obj)
    {
      bool isInitialized = true;
      while (this.watchDogToken.IsCancellationRequested == false)
      {
        this.TrySendPing();
        if (this.Client.IsConnected == false)
        {
          this.Logger.Warn("Slack connection down. Reconnecting");
          if (isInitialized)
          {
            // Don't deinit if Initialize failed
            this.DeinitializeConnection();
            this.ClearChannelsInfo();
            this.UpdateStatus();
          }

          isInitialized = this.InitializeConnection();
        }

        Thread.Sleep(WatchDogDelay);
      }
    }
  }
}
