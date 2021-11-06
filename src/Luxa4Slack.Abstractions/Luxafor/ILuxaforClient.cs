namespace CG.Luxa4Slack.Abstractions.Luxafor
{
  using System.Threading.Tasks;

  public interface ILuxaforClient
  {
    bool IsInitialized { get; }

    void SetBrightness(double value);

    Task<bool> SetAsync(LuxaforColor color, int timeout = 200);

    Task<bool> ResetAsync();

    Task<bool> TestAsync();

    Task<bool> StartWaveProcessingAsync();
  }
}
