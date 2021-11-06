namespace CG.Luxa4Slack.Tray.ViewModels
{
  using System;
  using System.Windows;
  using System.Windows.Input;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;
  using CG.Luxa4Slack.Tray.Views;
  using GalaSoft.MvvmLight;
  using GalaSoft.MvvmLight.Command;

  public class TrayIconViewModel : ViewModelBase
  {
    public TrayIconViewModel(PreferencesWindowController preferencesWindowController, ApplicationInfo applicationInfo)
    {
      ShowPreferencesCommand = new RelayCommand(() => preferencesWindowController.ShowDialog(), true);
      ExitApplicationCommand = new RelayCommand(() => Application.Current.Shutdown(), true);
      Icon = new BitmapImage(new Uri("pack://application:,,,/Luxa4Slack.Tray;component/Icon.ico"));
      ToolTip = applicationInfo.DisplayName;
    }

    public ImageSource Icon { get; }

    public string ToolTip { get; }

    public ICommand ShowPreferencesCommand { get; }

    public ICommand ExitApplicationCommand { get; }
  }
}
