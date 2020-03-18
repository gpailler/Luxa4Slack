namespace CG.Luxa4Slack.Tray
{
  using System;
  using System.Collections.ObjectModel;
  using System.Collections.Specialized;
  using System.Diagnostics;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows.Input;
  using CG.Luxa4Slack.NotificationClient;
  using GalaSoft.MvvmLight;
  using GalaSoft.MvvmLight.CommandWpf;

  public class PreferencesViewModel : ViewModelBase
  {
    private readonly Action preferencesUpdated;
    private readonly INotificationClient notificationClient;
    private int brightness;
    private string newToken;
    private CancellationTokenSource cancellationTokenSource;

    public PreferencesViewModel(Action preferencesUpdated)
    {
      this.preferencesUpdated = preferencesUpdated;
      this.UpdatePreferencesCommand = new RelayCommand(this.UpdatePreferences);
      this.RequestTokenCommand = new RelayCommand(() => Process.Start(OAuthHelper.GetAuthorizationUri().ToString()));
      this.RemoveTokenCommand = new RelayCommand<SlackToken>(x => this.Tokens.Remove(x));
      this.AddTokenCommand = new RelayCommand(() => this.AddToken());
      this.Title = $"{App.AppName} - Preferences";
      this.Tokens = new ObservableCollection<SlackToken>();
      this.ShowUnreadMentions = Properties.Settings.Default.ShowUnreadMentions;
      this.ShowUnreadMessages = Properties.Settings.Default.ShowUnreadMessages;
      this.ShowStatus = Properties.Settings.Default.ShowStatus;

      this.notificationClient = NotificationClientFactory.Create(1, Debugger.IsAttached);
      this.notificationClient.Initialize();
      this.BrightnessPercent = Properties.Settings.Default.Brighness;

      if (Properties.Settings.Default.Tokens != null)
      {
        foreach (var token in Properties.Settings.Default.Tokens)
        {
          this.Tokens.Add(new SlackToken(token));
        }
      }
    }

    public ICommand UpdatePreferencesCommand { get; }

    public ICommand RequestTokenCommand { get; }

    public ICommand RemoveTokenCommand { get; }

    public ICommand AddTokenCommand { get; }

    public string Title { get; }

    public ObservableCollection<SlackToken> Tokens { get; set; }

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

    public string NewToken
    {
      get { return this.newToken ?? string.Empty; }
      set
      {
        this.newToken = value;
        this.RaisePropertyChanged();
      }
    }

    private void UpdateLuxafor(double brightnessPercent)
    {
      // Cancel pending updates
      cancellationTokenSource?.Cancel();
      cancellationTokenSource = new CancellationTokenSource();

      Task.Run(async () =>
      {
        this.notificationClient.SetBrightness(brightnessPercent);
        await this.notificationClient.SetAsync(Colors.White, 100);
      }, cancellationTokenSource.Token);
    }

    private void UpdatePreferences()
    {
      var tokensCollection = new StringCollection();
      tokensCollection.AddRange(this.Tokens.Select(x => x.Token).ToArray());
      Properties.Settings.Default.Tokens = tokensCollection;
      Properties.Settings.Default.ShowUnreadMentions = this.ShowUnreadMentions;
      Properties.Settings.Default.ShowUnreadMessages = this.ShowUnreadMessages;
      Properties.Settings.Default.ShowStatus = this.ShowStatus;
      Properties.Settings.Default.Brighness = this.BrightnessPercent;
      Properties.Settings.Default.Save();

      this.preferencesUpdated?.Invoke();
    }

    private void AddToken()
    {
      var newToken = this.NewToken?.Trim();
      if (!string.IsNullOrEmpty(newToken))
      {
        this.Tokens.Add(new SlackToken(newToken));
        this.NewToken = null;
      }
    }

    public class SlackToken : ViewModelBase
    {
      public SlackToken(string token)
      {
        this.Token = token;
        this.Workspace = "Loading...";

        Task.Run(() =>
        {
          this.Workspace = WorkspaceHelper.GetWorkspace(token);
          this.RaisePropertyChanged(nameof(this.Workspace));
        });
      }

      public string Token { get; }

      public string Workspace { get; private set; }
    }
  }
}
