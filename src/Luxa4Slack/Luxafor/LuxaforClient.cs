namespace CG.Luxa4Slack.Luxafor
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using CG.Luxa4Slack.Abstractions.Luxafor;
  using LuxaforSharp;
  using Microsoft.Extensions.Logging;

  internal class LuxaforClient : IDisposable, ILuxaforClient
  {
    private static readonly Dictionary<LuxaforColor, Color> s_colorsMapping = new()
    {
      { LuxaforColor.None, new Color(0, 0, 0) },
      { LuxaforColor.White, new Color(255, 255, 255) },
      { LuxaforColor.Red, new Color(255, 0, 0) },
      { LuxaforColor.Green, new Color(0, 255, 0) },
      { LuxaforColor.Yellow, new Color(255, 255, 0) },
      { LuxaforColor.Blue, new Color(0, 0, 255) },
      { LuxaforColor.Cyan, new Color(0, 255, 255) },
      { LuxaforColor.Orange, new Color(0xFF, 0x66, 0) },
    };

    private readonly ILogger<LuxaforClient> _logger;

    private IDevice? _device;
    private double _brightness;

    public LuxaforClient(ILogger<LuxaforClient> logger)
    {
      _logger = logger;

      Initialize();
    }

    public bool IsInitialized => _device != null;

    private void Initialize()
    {
      IDeviceList list = new DeviceList();
      list.Scan();
      _logger.LogDebug("Found {0} devices", list.Count());

      _device = list.FirstOrDefault();
      _logger.LogDebug("Selected device: {0}", (_device as Device)?.DevicePath ?? "None");
    }

    public void Dispose()
    {
      if (_device != null)
      {
        Task.Factory
          .StartNew(async () => await ResetAsync())
          .Unwrap()
          .GetAwaiter()
          .GetResult();

        _device.Dispose();
        _device = null;
      }
    }

    public void SetBrightness(double value)
    {
      _brightness = Math.Max(0, Math.Min(1, value));
    }

    public async Task<bool> SetAsync(LuxaforColor color, int timeout = 200)
    {
      if (_device == null)
      {
        throw new InvalidOperationException("Not initialized");
      }

      _logger.LogDebug("Set color: {0} @ {1:P0}", color, _brightness);

      var luxaforFinalColor = new Color(
        (byte)(s_colorsMapping[color].Red * _brightness),
        (byte)(s_colorsMapping[color].Green * _brightness),
        (byte)(s_colorsMapping[color].Blue * _brightness));

      return await _device.AllLeds.SetColor(luxaforFinalColor, timeout: timeout);
    }

    public async Task<bool> ResetAsync()
    {
      return await SetAsync(LuxaforColor.None);
    }

    public async Task<bool> TestAsync()
    {
      _logger.LogDebug("Test device");

      var result = await SetAsync(LuxaforColor.White);
      await Task.Delay(200);
      return result && await SetAsync(LuxaforColor.None);
    }

    public async Task<bool> StartWaveProcessingAsync()
    {
      if (_device == null)
      {
        throw new InvalidOperationException("Not initialized");
      }

      return await _device.Wave(WaveType.OverlappingShort, s_colorsMapping[LuxaforColor.Yellow], 4, byte.MaxValue);
    }
  }
}
