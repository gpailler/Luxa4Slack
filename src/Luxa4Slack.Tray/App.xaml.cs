namespace CG.Luxa4Slack.Tray
{
  using System;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Windows;

  using CG.Luxa4Slack.Tray.Properties;

  using Hardcodet.Wpf.TaskbarNotification;

  public partial class App : Application
  {
    public const string AppName = "Luxa4Slack";

    private TaskbarIcon notifyIcon;

    private TrayViewModel viewModel;

    private Luxa4Slack luxa4Slack;

    protected override async void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      // Init tray
      this.notifyIcon = (TaskbarIcon)this.FindResource("NotifyIcon");
      this.viewModel = new TrayViewModel(this.ReInitializeLuxa4Slack);
      this.notifyIcon.DataContext = this.viewModel;

      // Upgrade settings if needed
      if (Settings.Default.ShouldUpgrade)
      {
        Settings.Default.Upgrade();
        Settings.Default.ShouldUpgrade = false;
        Settings.Default.Save();
      }

      // Init Luxa4Slack
      await this.InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
      this.luxa4Slack?.Dispose();
      this.notifyIcon.Dispose();

      base.OnExit(e);
    }

    private void ReInitializeLuxa4Slack()
    {
      this.luxa4Slack?.Dispose();
      Task.Factory.StartNew(async () => await this.InitializeAsync());
    }

    private async Task InitializeAsync()
    {
      if (Settings.Default.Tokens == null || Settings.Default.Tokens.Count == 0)
      {
        this.Dispatcher.Invoke(() => this.viewModel.ShowPreferencesCommand.Execute(null));
      }
      else
      {
        this.luxa4Slack = new Luxa4Slack(
          Settings.Default.Tokens.OfType<string>(),
          Settings.Default.ShowUnreadMentions,
          Settings.Default.ShowUnreadMessages,
          Settings.Default.ShowStatus,
          Settings.Default.Brighness);
        try
        {
          await this.luxa4Slack.Initialize();
          this.luxa4Slack.LuxaforFailure += this.OnLuxaforFailure;
        }
        catch (Exception ex)
        {
          this.viewModel.ShowError($"Unable to initialize Luxa4Slack: {ex.Message}");
          this.Dispatcher.Invoke(() => this.viewModel.ExitApplicationCommand.Execute(null));
        }
      }
    }

    private void OnLuxaforFailure()
    {
      this.viewModel.ShowError("Luxafor communication issue. Please unplug/replug the Luxafor and restart the application");
    }
  }
}
