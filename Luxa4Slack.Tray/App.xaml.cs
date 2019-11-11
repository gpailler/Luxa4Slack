namespace CG.Luxa4Slack.Tray
{
  using System;
  using System.Threading.Tasks;
  using System.Windows;

  using CG.Luxa4Slack.Tray.Properties;

  using Hardcodet.Wpf.TaskbarNotification;

  public partial class App : Application
  {
    public const string AppName = "Luxa4Slack";

    private TaskbarIcon notifyIcon;

    private TrayViewModel viewModel;

    private string token;

    private bool showUnreadMentions;

    private bool showUnreadMessages;

    private bool showStatus;

    private Luxa4Slack luxa4Slack;

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      // Init tray
      this.notifyIcon = (TaskbarIcon)this.FindResource("NotifyIcon");
      this.viewModel = new TrayViewModel();
      this.notifyIcon.DataContext = this.viewModel;

      // Upgrade settings if needed
      if (Settings.Default.ShouldUpgrade)
      {
        Settings.Default.Upgrade();
        Settings.Default.ShouldUpgrade = false;
        Settings.Default.Save();
      }

      this.token = Settings.Default.Token;
      this.showUnreadMentions = Settings.Default.ShowUnreadMentions;
      this.showUnreadMessages = Settings.Default.ShowUnreadMessages;
      this.showStatus = Settings.Default.ShowStatus;

      // Init Luxa4Slack
      Task.Factory.StartNew(this.Initialize);

    }

    protected override void OnExit(ExitEventArgs e)
    {
      this.luxa4Slack?.Dispose();
      this.notifyIcon.Dispose();

      base.OnExit(e);
    }

    private void Initialize()
    {
      if (string.IsNullOrWhiteSpace(this.token))
      {
        this.Dispatcher.Invoke(() => this.viewModel.ShowPreferencesCommand.Execute(null));
      }
      else
      {
        this.luxa4Slack = new Luxa4Slack(this.token, this.showUnreadMentions, this.showUnreadMessages, this.showStatus);
        try
        {
          this.luxa4Slack.Initialize();
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
