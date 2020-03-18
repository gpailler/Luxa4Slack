namespace CG.Luxa4Slack.NotificationClient
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using LuxaforSharp;

  using NLog;

  public class LuxaforClient : INotificationClient, IDisposable
  {
    private const int Timeout = 200;

    private readonly ILogger logger = LogManager.GetLogger("Luxafor");
    private readonly Dictionary<Colors, Color> colorsMapping = new Dictionary<Colors, Color>
                                                        {
                                                          { Colors.None, new Color(0, 0, 0) },
                                                          { Colors.White, new Color(255, 255, 255) },
                                                          { Colors.Red, new Color(255, 0, 0) },
                                                          { Colors.Green, new Color(0, 255, 0) },
                                                          { Colors.Yellow, new Color(255, 255, 0) },
                                                          { Colors.Blue, new Color(0, 0, 255) },
                                                          { Colors.Cyan, new Color(0, 255, 255) },
                                                          { Colors.Orange, new Color(0xFF, 0x66, 0) },
                                                        };

    private IDevice device;
    private double brightness;

    public LuxaforClient(double brightness)
    {
      this.SetBrightness(brightness);
    }

    public bool Initialize()
    {
      IDeviceList list = new DeviceList();
      list.Scan();
      this.logger.Debug("Found {0} devices", list.Count());

      this.device = list.FirstOrDefault();
      this.logger.Debug("Selected device: {0}", (this.device as Device)?.DevicePath ?? "None");

      return this.device != null;
    }

    public void Dispose()
    {
      this.device?.Dispose();
    }

    public void SetBrightness(double brightness)
    {
      this.brightness = Math.Max(0, Math.Min(1, brightness));
    }

    public async Task<bool> SetAsync(Colors color, int? timeout = null)
    {
      if (!timeout.HasValue)
      {
        timeout = Timeout;
      }

      if (this.device == null)
      {
        throw new InvalidOperationException("Not initialized");
      }
      else
      {
        this.logger.Debug("Set color: {0} @ {1:P0}", color, this.brightness);

        var luxaforFinalColor = new Color(
          (byte) (this.colorsMapping[color].Red * this.brightness),
          (byte) (this.colorsMapping[color].Green * this.brightness),
          (byte) (this.colorsMapping[color].Blue * this.brightness));

        return await this.device.AllLeds.SetColor(luxaforFinalColor, timeout: timeout.Value);
      }
    }

    public async Task<bool> ResetAsync()
    {
      return await this.SetAsync(Colors.None);
    }

    public async Task<bool> TestAsync()
    {
      this.logger.Debug("Test device");

      var result = await this.SetAsync(Colors.White);
      await Task.Delay(200);
      return result && await this.SetAsync(Colors.None);
    }

    public async Task<bool> StartWaveProcessingAsync()
    {
      return await this.device.Wave(WaveType.OverlappingShort, colorsMapping[Colors.Yellow], 4, byte.MaxValue);
    }
  }
}
