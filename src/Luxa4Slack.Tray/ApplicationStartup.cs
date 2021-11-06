﻿namespace CG.Luxa4Slack.Tray
{
  using System;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Threading;
  using CG.Luxa4Slack.Tray.Options;
  using CG.Luxa4Slack.Tray.Views;
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

    private Luxa4Slack? _luxa4Slack;

    public ApplicationStartup(
      IOptionsMonitor<ApplicationOptions> options,
      TrayIconController trayIconController,
      PreferencesWindowController preferencesWindowController,
      ApplicationInfo applicationInfo,
      Lazy<Dispatcher> dispatcher,
      ILogger<ApplicationStartup> logger)
    {
      _options = options;
      _trayIconController = trayIconController;
      _preferencesWindowController = preferencesWindowController;
      _applicationInfo = applicationInfo;
      _dispatcher = dispatcher;
      _logger = logger;

      _preferencesWindowController.OpenedChanged += OnPreferencesWindowWindowOpenedChanged;
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
        _luxa4Slack = new Luxa4Slack(
          _options.CurrentValue.Tokens,
          _options.CurrentValue.ShowUnreadMentions,
          _options.CurrentValue.ShowUnreadMessages,
          _options.CurrentValue.ShowStatus,
          _options.CurrentValue.Brightness);

        _luxa4Slack.LuxaforFailure += OnLuxaforFailure;

        try
        {
          await _luxa4Slack.Initialize();
        }
        catch (Exception ex)
        {
          ShowError($"Unable to initialize Luxa4Slack: {ex.Message}", ex);
        }
      }
    }

    private void OnLuxaforFailure()
    {
      ShowError("Luxafor communication issue. Please unplug/replug the Luxafor and restart the application");
    }

    private void ShowError(string message, Exception? ex = null)
    {
      _logger.LogError(ex, message);
      MessageBox.Show(message, _applicationInfo.Format("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
      Application.Current.Shutdown();
    }

    private void OnPreferencesWindowWindowOpenedChanged(bool opened)
    {
      if (opened)
      {
        DeInitialize();
      }
      else
      {
        Initialize();
      }
    }
  }
}