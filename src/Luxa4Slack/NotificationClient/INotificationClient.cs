namespace CG.Luxa4Slack.NotificationClient
{
  using System.Threading;
  using System.Threading.Tasks;

  public interface INotificationClient
  {
    bool Initialize();

    void SetBrightness(double brightness);

    Task<bool> SetAsync(Colors color, int? timeout = null);

    Task<bool> ResetAsync();

    Task<bool> TestAsync();

    Task<bool> StartWaveProcessingAsync();
  }
}
