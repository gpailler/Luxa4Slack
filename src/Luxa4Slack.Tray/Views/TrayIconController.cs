namespace CG.Luxa4Slack.Tray.Views
{
  using System;
  using System.Windows;
  using CG.Luxa4Slack.Tray.ViewModels;
  using Hardcodet.Wpf.TaskbarNotification;

  public class TrayIconController : IDisposable
  {
    private readonly TrayIconViewModel trayIconViewModel;
    private TaskbarIcon? notifyIcon;

    public TrayIconController(TrayIconViewModel trayIconViewModel)
    {
      this.trayIconViewModel = trayIconViewModel;
    }

    public void Init()
    {
      this.notifyIcon = (TaskbarIcon)Application.Current.FindResource("NotifyIcon")!;
      this.notifyIcon.DataContext = trayIconViewModel;
    }

    public void Dispose()
    {
      this.notifyIcon?.Dispose();
    }
  }
}
