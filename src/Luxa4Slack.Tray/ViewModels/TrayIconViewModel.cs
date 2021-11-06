namespace CG.Luxa4Slack.Tray.ViewModels
{
  using System;
  using System.Windows;
  using System.Windows.Input;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;
  using CG.Luxa4Slack.Tray.Views;
  using Microsoft.Toolkit.Mvvm.ComponentModel;
  using Microsoft.Toolkit.Mvvm.Input;

  public class TrayIconViewModel : ObservableObject
  {
    private readonly Lazy<ImageSource> _lazyIcon;

    public TrayIconViewModel(PreferencesWindowController preferencesWindowController, ApplicationInfo applicationInfo)
    {
      ShowPreferencesCommand = new RelayCommand(() => preferencesWindowController.ShowDialog());
      ExitApplicationCommand = new RelayCommand(() => Application.Current.Shutdown());
      _lazyIcon = new Lazy<ImageSource>(() => new BitmapImage(new Uri("pack://application:,,,/Luxa4Slack.Tray;component/Icon.ico")));
      ToolTip = applicationInfo.DisplayName;
    }

    public ImageSource Icon => _lazyIcon.Value;

    public string ToolTip { get; }

    public ICommand ShowPreferencesCommand { get; }

    public ICommand ExitApplicationCommand { get; }
  }
}
