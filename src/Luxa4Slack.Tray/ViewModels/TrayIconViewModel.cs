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
    public TrayIconViewModel(PreferencesWindowFactory preferencesWindowFactory, ApplicationInfo applicationInfo)
    {
      this.ShowPreferencesCommand = new RelayCommand(() => preferencesWindowFactory.ShowDialog(), true);
      this.ExitApplicationCommand = new RelayCommand(() => Application.Current.Shutdown(), true);
      this.ToolTip = applicationInfo.DisplayName;
    }

    public ImageSource Icon => new BitmapImage(new Uri("pack://application:,,,/Luxa4Slack.Tray;component/Icon.ico"));

    public string ToolTip { get; }

    public ICommand ShowPreferencesCommand { get; }

    public ICommand ExitApplicationCommand { get; }
  }
}
