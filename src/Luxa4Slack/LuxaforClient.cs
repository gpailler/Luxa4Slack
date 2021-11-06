namespace CG.Luxa4Slack
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using CG.Luxa4Slack.Abstractions;
  using LuxaforSharp;
  using Microsoft.Extensions.Logging;

  internal class LuxaforClient : IDisposable, ILuxaforClient
  {
    private readonly ILogger<LuxaforClient> logger;

    private readonly Dictionary<LuxaforColor, Color> colorsMapping = new Dictionary<LuxaforColor, Color>
    {
      {LuxaforColor.None, new Color(0, 0, 0)},
      {LuxaforColor.White, new Color(255, 255, 255)},
      {LuxaforColor.Red, new Color(255, 0, 0)},
      {LuxaforColor.Green, new Color(0, 255, 0)},
      {LuxaforColor.Yellow, new Color(255, 255, 0)},
      {LuxaforColor.Blue, new Color(0, 0, 255)},
      {LuxaforColor.Cyan, new Color(0, 255, 255)},
      {LuxaforColor.Orange, new Color(0xFF, 0x66, 0)},
    };

    private IDevice? device;
    private double brightness;

    public LuxaforClient(ILogger<LuxaforClient> logger)
    {
      this.logger = logger;

      this.Initialize();
    }

    public bool IsInitialized => this.device != null;

    private void Initialize()
    {
      IDeviceList list = new DeviceList();
      list.Scan();
      this.logger.LogDebug("Found {0} devices", list.Count());

      this.device = list.FirstOrDefault();
      this.logger.LogDebug("Selected device: {0}", (this.device as Device)?.DevicePath ?? "None");
    }

    public void Dispose()
    {
      if (this.device != null)
      {
        Task.Factory
          .StartNew(async () => await this.ResetAsync())
          .Unwrap()
          .GetAwaiter()
          .GetResult();

        this.device.Dispose();
        this.device = null;
      }
    }

    public void SetBrightness(double value)
    {
      this.brightness = Math.Max(0, Math.Min(1, value));
    }

    public async Task<bool> SetAsync(LuxaforColor color, int timeout = 200)
    {
      if (this.device == null)
      {
        throw new InvalidOperationException("Not initialized");
      }

      this.logger.LogDebug("Set color: {0} @ {1:P0}", color, this.brightness);

      var luxaforFinalColor = new Color(
        (byte) (this.colorsMapping[color].Red * this.brightness),
        (byte) (this.colorsMapping[color].Green * this.brightness),
        (byte) (this.colorsMapping[color].Blue * this.brightness));

      return await this.device.AllLeds.SetColor(luxaforFinalColor, timeout: timeout);
    }

    public async Task<bool> ResetAsync()
    {
      return await this.SetAsync(LuxaforColor.None);
    }

    public async Task<bool> TestAsync()
    {
      this.logger.LogDebug("Test device");

      var result = await this.SetAsync(LuxaforColor.White);
      await Task.Delay(200);
      return result && await this.SetAsync(LuxaforColor.None);
    }

    public async Task<bool> StartWaveProcessingAsync()
    {
      if (this.device == null)
      {
        throw new InvalidOperationException("Not initialized");
      }

      return await this.device.Wave(WaveType.OverlappingShort, colorsMapping[LuxaforColor.Yellow], 4, byte.MaxValue);
    }
  }
}
