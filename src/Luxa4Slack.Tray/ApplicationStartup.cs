namespace CG.Luxa4Slack.Tray
{
  using System;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Threading;
  using CG.Luxa4Slack.Abstractions;
  using CG.Luxa4Slack.Abstractions.Luxafor;
  using CG.Luxa4Slack.Tray.Options;
  using CG.Luxa4Slack.Tray.Views;
  using Microsoft.Extensions.Configuration;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Options;

  public class ApplicationStartup : IDisposable
  {
    private readonly IOptionsMonitor<ApplicationOptions> _options;
    private readonly TrayIconController _trayIconController;
    private readonly PreferencesWindowController _preferencesWindowController;
    private readonly ApplicationInfo _applicationInfo;
    private readonly Lazy<Dispatcher> _dispatcher;
    private readonly ILogger _logger;
    private readonly ILuxa4SlackFactory _luxa4SlackFactory;
    private readonly ILuxaforClient _luxaforClient;
    private readonly IConfigurationRoot _configurationRoot;

    private ILuxa4Slack? _luxa4Slack;

    public ApplicationStartup(
      IOptionsMonitor<ApplicationOptions> options,
      TrayIconController trayIconController,
      PreferencesWindowController preferencesWindowController,
      ApplicationInfo applicationInfo,
      Lazy<Dispatcher> dispatcher,
      ILogger<ApplicationStartup> logger,
      ILuxa4SlackFactory luxa4SlackFactory,
      ILuxaforClient luxaforClient,
      IConfigurationRoot configurationRoot)
    {
      _options = options;
      _trayIconController = trayIconController;
      _preferencesWindowController = preferencesWindowController;
      _applicationInfo = applicationInfo;
      _dispatcher = dispatcher;
      _logger = logger;
      _luxa4SlackFactory = luxa4SlackFactory;
      _luxaforClient = luxaforClient;
      _configurationRoot = configurationRoot;

      _preferencesWindowController.OpenedChanged += OnPreferencesWindowWindowOpenedChanged;
      _luxaforClient.LuxaforFailed += OnLuxaforFailed;
    }

    public void Run()
    {
      _logger.LogInformation($"Starting {_applicationInfo.DisplayName}");

      try
      {
        var app = new App();
        app.InitializeComponent();

        _dispatcher.Value.Invoke(() => _trayIconController.Init());

        Initialize();

        app.Run();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error while running.");
      }
    }

    public void Dispose()
    {
      _preferencesWindowController.OpenedChanged -= OnPreferencesWindowWindowOpenedChanged;
      _luxaforClient.LuxaforFailed -= OnLuxaforFailed;
      DeInitialize();
    }

    private void Initialize()
    {
      Task.Run(async () => await InitializeAsync());
    }

    private void DeInitialize()
    {
      _luxa4Slack?.Dispose();
      _luxa4Slack = null;
    }

    private async Task InitializeAsync()
    {
      _logger.LogInformation("Initializing");

      _luxa4Slack?.Dispose();

      if (_options.CurrentValue.Tokens.Length == 0)
      {
        _dispatcher.Value.Invoke(() => _preferencesWindowController.ShowDialog());
      }
      else
      {
        _luxa4Slack = _luxa4SlackFactory.Create(
          _options.CurrentValue.Tokens,
          _options.CurrentValue.ShowUnreadMentions,
          _options.CurrentValue.ShowUnreadMessages,
          _options.CurrentValue.ShowStatus,
          _options.CurrentValue.Brightness);

        try
        {
          await _luxa4Slack.InitializeAsync();
        }
        catch (Exception ex)
        {
          ShowError($"Unable to initialize Luxa4Slack: {ex.Message}", ex);
        }
      }
    }

    private void OnLuxaforFailed()
    {
      _luxaforClient.LuxaforFailed -= OnLuxaforFailed;
      ShowError("Luxafor communication issue. Please unplug/replug the Luxafor device and restart the application");
    }

    private void ShowError(string message, Exception? ex = null)
    {
      _logger.LogError(ex, message);
      MessageBox.Show(message, _applicationInfo.Format("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
      _dispatcher.Value.Invoke(() => Application.Current.Shutdown());
    }

    private void OnPreferencesWindowWindowOpenedChanged(bool opened)
    {
      if (opened)
      {
        DeInitialize();
      }
      else
      {
        _configurationRoot.Reload();
        Initialize();
      }
    }
  }
}
