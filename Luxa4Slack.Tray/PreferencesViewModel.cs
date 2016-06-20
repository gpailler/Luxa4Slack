namespace CG.Luxa4Slack.Tray
{
  using System.Windows;
  using System.Windows.Input;

  using GalaSoft.MvvmLight;
  using GalaSoft.MvvmLight.CommandWpf;

  public class PreferencesViewModel : ViewModelBase
  {
    public PreferencesViewModel()
    {
      this.RestartApplicationCommand = new RelayCommand(this.RestartApplication);
      this.Title = $"{App.AppName} - Preferences";
      this.Token = Properties.Settings.Default.Token;
      this.ShowUnreadMentions = Properties.Settings.Default.ShowUnreadMentions;
      this.ShowUnreadMessages = Properties.Settings.Default.ShowUnreadMessages;
    }

    public ICommand RestartApplicationCommand { get; private set; }

    public string Title { get; }

    public string Token { get; set; }

    public bool ShowUnreadMentions { get; set; }

    public bool ShowUnreadMessages { get; set; }

    private void RestartApplication()
    {
      Properties.Settings.Default.Token = this.Token;
      Properties.Settings.Default.ShowUnreadMentions = this.ShowUnreadMentions;
      Properties.Settings.Default.ShowUnreadMessages = this.ShowUnreadMessages;
      Properties.Settings.Default.Save();

      System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
      Application.Current.Shutdown();
    }
  }
}
