namespace CG.Luxa4Slack.Tray
{
  using System;
  using System.Diagnostics;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows.Input;

  using GalaSoft.MvvmLight;
  using GalaSoft.MvvmLight.CommandWpf;

  public class PreferencesViewModel : ViewModelBase
  {
    private readonly Action preferencesUpdated;
    private readonly LuxaforClient luxaforClient;
    private int brightness;
    private CancellationTokenSource cancellationTokenSource;

    public PreferencesViewModel(Action preferencesUpdated)
    {
      this.preferencesUpdated = preferencesUpdated;
      this.UpdatePreferencesCommand = new RelayCommand(this.UpdatePreferences);
      this.RequestTokenCommand = new RelayCommand(() => Process.Start(OAuthHelper.GetAuthorizationUri().ToString()));
      this.Title = $"{App.AppName} - Preferences";
      this.Token = Properties.Settings.Default.Token;
      this.ShowUnreadMentions = Properties.Settings.Default.ShowUnreadMentions;
      this.ShowUnreadMessages = Properties.Settings.Default.ShowUnreadMessages;
      this.ShowStatus = Properties.Settings.Default.ShowStatus;

      this.luxaforClient = new LuxaforClient();
      this.luxaforClient.Initialize();
      this.BrightnessPercent = Properties.Settings.Default.Brighness;
    }

    public ICommand UpdatePreferencesCommand { get; private set; }

    public ICommand RequestTokenCommand { get; private set; }

    public string Title { get; }

    public string Token { get; set; }

    public bool ShowUnreadMentions { get; set; }

    public bool ShowUnreadMessages { get; set; }

    public bool ShowStatus { get; set; }

    public int Brightness
    {
      get { return this.brightness; }
      set
      {
        if (this.brightness != value)
        {
          this.brightness = value;
          this.RaisePropertyChanged(nameof(this.BrightnessPercent));

          this.UpdateLuxafor(this.BrightnessPercent);
        }
      }
    }

    public double BrightnessPercent
    {
      get => this.Brightness / 100d;
      set => this.Brightness = (int)(value * 100d);
    }
    
    private void UpdateLuxafor(double brightnessPercent)
    {
      // Cancel pending updates
      cancellationTokenSource?.Cancel();
      cancellationTokenSource = new CancellationTokenSource();

      Task.Run(async () =>
      {
        this.luxaforClient.SetBrightness(brightnessPercent);
        await this.luxaforClient.SetAsync(LuxaforClient.Colors.White, 100);
      }, cancellationTokenSource.Token);
    }

    private void UpdatePreferences()
    {
      Properties.Settings.Default.Token = this.Token;
      Properties.Settings.Default.ShowUnreadMentions = this.ShowUnreadMentions;
      Properties.Settings.Default.ShowUnreadMessages = this.ShowUnreadMessages;
      Properties.Settings.Default.ShowStatus = this.ShowStatus;
      Properties.Settings.Default.Brighness = this.BrightnessPercent;
      Properties.Settings.Default.Save();

      this.preferencesUpdated?.Invoke();
    }
  }
}
