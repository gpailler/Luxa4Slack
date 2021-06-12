namespace CG.Luxa4Slack.Tray
{
  using System;
  using System.Windows;
  using System.Windows.Input;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;

  using GalaSoft.MvvmLight;
  using GalaSoft.MvvmLight.Command;

  public class TrayViewModel : ViewModelBase
  {
    private readonly Action preferencesUpdated;
    private readonly BitmapImage icon = new BitmapImage(new Uri("pack://application:,,,/Luxa4Slack.Tray;component/Icon.ico"));
    private readonly BitmapImage iconError = new BitmapImage(new Uri("pack://application:,,,/Luxa4Slack.Tray;component/IconError.ico"));

    public TrayViewModel(Action preferencesUpdated)
    {
      this.preferencesUpdated = preferencesUpdated;
      this.ShowPreferencesCommand = new RelayCommand(this.ShowPreferences, () => Application.Current.MainWindow == null);
      this.ExitApplicationCommand = new RelayCommand(() => Application.Current.Shutdown());
      this.Icon = this.icon;
    }

    public ImageSource Icon { get; private set; }

    public ICommand ShowPreferencesCommand { get; private set; }

    public ICommand ExitApplicationCommand { get; private set; }

    public void ShowError(string message)
    {
      MessageBox.Show(message, $"{App.AppName} - Error", MessageBoxButton.OK, MessageBoxImage.Error);

      this.Icon = this.iconError;
      this.RaisePropertyChanged(() => this.Icon);
    }

    private void ShowPreferences()
    {
      Application.Current.MainWindow = new Preferences();
      Application.Current.MainWindow.DataContext = new PreferencesViewModel(preferencesUpdated);
      Application.Current.MainWindow.Show();
    }
  }
}
