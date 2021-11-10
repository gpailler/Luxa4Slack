namespace CG.Luxa4Slack.Tray.Views
{
  using System;
  using System.Windows;
  using CG.Luxa4Slack.Tray.ViewModels;
  using Hardcodet.Wpf.TaskbarNotification;

  public class TrayIconController : IDisposable
  {
    private readonly TrayIconViewModel _trayIconViewModel;
    private TaskbarIcon? _notifyIcon;

    public TrayIconController(TrayIconViewModel trayIconViewModel)
    {
      _trayIconViewModel = trayIconViewModel;
    }

    public void Init()
    {
      _notifyIcon = (TaskbarIcon)Application.Current.FindResource("NotifyIcon")!;
      _notifyIcon.DataContext = _trayIconViewModel;
    }

    public void Dispose()
    {
      _notifyIcon?.Dispose();
    }
  }
}
