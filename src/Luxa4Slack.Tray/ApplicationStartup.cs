namespace CG.Luxa4Slack.Tray
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
    private readonly IOptionsMonitor<ApplicationOptions> options;
    private readonly TrayIconController trayIconController;
    private readonly PreferencesWindowFactory preferencesWindowFactory;
    private readonly ApplicationInfo applicationInfo;
    private readonly Lazy<Dispatcher> dispatcher;
    private readonly ILogger logger;

    private Luxa4Slack? luxa4Slack;

    public ApplicationStartup(
      IOptionsMonitor<ApplicationOptions> options,
      TrayIconController trayIconController,
      PreferencesWindowFactory preferencesWindowFactory,
      ApplicationInfo applicationInfo,
      Lazy<Dispatcher> dispatcher,
      ILogger<ApplicationStartup> logger)
    {
      this.options = options;
      this.trayIconController = trayIconController;
      this.preferencesWindowFactory = preferencesWindowFactory;
      this.applicationInfo = applicationInfo;
      this.dispatcher = dispatcher;
      this.logger = logger;

      this.preferencesWindowFactory.OpenedChanged += OnPreferencesWindowWindowOpenedChanged;
    }

    public void Run()
    {
      this.logger.LogInformation($"Starting {this.applicationInfo.DisplayName}");

      try
      {
        var app = new App();
        app.InitializeComponent();

        this.dispatcher.Value.Invoke(() => this.trayIconController.Init());

        this.Initialize();

        app.Run();
      }
      catch (Exception ex)
      {
        this.logger.LogError(ex, "Error while running.");
      }
    }

    public void Dispose()
    {
      this.DeInitialize();
    }

    private void Initialize()
    {
      Task.Run(async () => await this.InitializeAsync());
    }

    private void DeInitialize()
    {
      this.luxa4Slack?.Dispose();
      this.luxa4Slack = null;
    }

    private async Task InitializeAsync()
    {
      this.logger.LogInformation("Initializing");

      this.luxa4Slack?.Dispose();

      if (this.options.CurrentValue.Tokens.Length == 0)
      {
        this.dispatcher.Value.Invoke(() => this.preferencesWindowFactory.ShowDialog());
      }
      else
      {
        this.luxa4Slack = new Luxa4Slack(
          this.options.CurrentValue.Tokens,
          this.options.CurrentValue.ShowUnreadMentions,
          this.options.CurrentValue.ShowUnreadMessages,
          this.options.CurrentValue.ShowStatus,
          this.options.CurrentValue.Brightness);

        this.luxa4Slack.LuxaforFailure += this.OnLuxaforFailure;

        try
        {
          await this.luxa4Slack.Initialize();
        }
        catch (Exception ex)
        {
          this.ShowError($"Unable to initialize Luxa4Slack: {ex.Message}", ex);
        }
      }
    }

    private void OnLuxaforFailure()
    {
      this.ShowError("Luxafor communication issue. Please unplug/replug the Luxafor and restart the application");
    }

    private void ShowError(string message, Exception? ex = null)
    {
      this.logger.LogError(ex, message);
      MessageBox.Show(message, applicationInfo.Format("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
      Application.Current.Shutdown();
    }

    private void OnPreferencesWindowWindowOpenedChanged(bool opened)
    {
      if (opened)
      {
        this.DeInitialize();
      }
      else
      {
        this.Initialize();
      }
    }
  }
}
