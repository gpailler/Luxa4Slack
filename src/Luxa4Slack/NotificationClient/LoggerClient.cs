namespace CG.Luxa4Slack.NotificationClient
{
  using System;
  using System.Threading.Tasks;

  using NLog;

  public class LoggerClient : INotificationClient, IDisposable
  {
    private readonly ILogger logger = LogManager.GetLogger("Luxafor.LoggerClient");
    private LuxaforClient luxaforClient;

    public LoggerClient(double brightness)
    {
      this.SetBrightness(brightness);
      this.luxaforClient = new LuxaforClient(brightness);
    }

    public bool Initialize()
    {
      this.logger.Debug($"{nameof(this.Initialize)}()");
      if (!this.luxaforClient.Initialize())
      {
        this.logger.Warn("Luxafor notification client is not initialized");
        this.luxaforClient = null;
      }

      return true;
    }

    public void Dispose()
    {
      this.logger.Debug($"{nameof(this.Dispose)}()");
      this.luxaforClient?.Dispose();
    }

    public void SetBrightness(double brightness)
    {
      this.logger.Debug($"{nameof(this.SetBrightness)}(brightness: {brightness})");
      this.luxaforClient?.SetBrightness(brightness);
    }

    public async Task<bool> SetAsync(Colors color, int? timeout = null)
    {
      this.logger.Debug($"{nameof(this.SetAsync)}(color: {color}, timeout: {timeout})");
      await (this.luxaforClient?.SetAsync(color, timeout) ?? Task.FromResult(true));
      return await Task.FromResult(true);
    }

    public async Task<bool> ResetAsync()
    {
      this.logger.Debug($"{nameof(this.ResetAsync)}()");
      await (this.luxaforClient?.ResetAsync() ?? Task.FromResult(true));
      return await Task.FromResult(true);
    }

    public async Task<bool> TestAsync()
    {
      this.logger.Debug($"{nameof(this.TestAsync)}()");
      await (this.luxaforClient?.TestAsync() ?? Task.FromResult(true));
      return await Task.FromResult(true);
    }

    public async Task<bool> StartWaveProcessingAsync()
    {
      this.logger.Debug($"{nameof(this.StartWaveProcessingAsync)}()");
      await (this.luxaforClient?.StartWaveProcessingAsync() ?? Task.FromResult(true));
      return await Task.FromResult(true);
    }
  }
}
