namespace CG.Luxa4Slack.Abstractions.Luxafor
{
  using System;
  using System.Threading.Tasks;

  public interface ILuxaforClient
  {
    bool IsInitialized { get; }

    event Action LuxaforFailed;

    void SetBrightness(double value);

    Task SetAsync(LuxaforColor color);

    Task ResetAsync();

    Task StartWaveProcessingAsync();
  }
}
