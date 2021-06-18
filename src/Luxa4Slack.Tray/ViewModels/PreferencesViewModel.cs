namespace CG.Luxa4Slack.Tray.ViewModels
{
  using System.Collections.ObjectModel;
  using System.Diagnostics;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows.Input;
  using CG.Luxa4Slack.Extensions;
  using CG.Luxa4Slack.Tray.Options;
  using GalaSoft.MvvmLight;
  using GalaSoft.MvvmLight.Command;
  using GalaSoft.MvvmLight.Messaging;

  public class PreferencesViewModel : ViewModelBase
  {
    private readonly IWritableOptions<ApplicationOptions> options;
    private readonly LuxaforClient luxaforClient;

    private int brightness;
    private string newToken = string.Empty;
    private CancellationTokenSource? cancellationTokenSource;

    public PreferencesViewModel(IWritableOptions<ApplicationOptions> options, ApplicationInfo applicationInfo, IMessenger messenger)
    {
      this.options = options;
      this.Title = applicationInfo.Format("Preferences");
      this.Tokens = new ObservableCollection<SlackToken>();

      this.UpdatePreferencesCommand = new RelayCommand(() =>
      {
        this.SaveSettings();
        this.CloseCommand?.Execute(null);
      });
      this.CloseCommand = new RelayCommand(() => messenger.Send(new CloseWindowMessage()), true);
      this.RequestTokenCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo(OAuthHelper.GetAuthorizationUri().ToString()) { UseShellExecute = true }));
      this.RemoveTokenCommand = new RelayCommand<SlackToken>(x => this.Tokens.Remove(x));
      this.AddTokenCommand = new RelayCommand(() => this.AddToken());

      this.luxaforClient = new LuxaforClient();
      this.luxaforClient.Initialize();

      this.LoadSettings();
    }

    public ICommand UpdatePreferencesCommand { get; }

    public ICommand CloseCommand { get; }

    public ICommand RequestTokenCommand { get; }

    public ICommand RemoveTokenCommand { get; }

    public ICommand AddTokenCommand { get; }

    public string Title { get; }

    public ObservableCollection<SlackToken> Tokens { get; }

    public bool ShowUnreadMentions { get; set; }

    public bool ShowUnreadMessages { get; set; }

    public bool ShowStatus { get; set; }

    public int Brightness
    {
      get { return this.brightness; }
      set
      {
        if (this.Set(ref this.brightness, value))
        {
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
      get { return this.newToken; }
      set { this.Set(ref this.newToken, value); }
    }

    public override void Cleanup()
    {
      cancellationTokenSource?.Cancel();
      this.luxaforClient.Dispose();

      base.Cleanup();
    }

    private void UpdateLuxafor(double brightnessPercent)
    {
      // Cancel pending updates
      cancellationTokenSource?.Cancel();
      cancellationTokenSource = new CancellationTokenSource();

      Task.Run(async () =>
      {
        this.luxaforClient.SetBrightness(brightnessPercent);
        await this.luxaforClient.SetAsync(LuxaforClient.Colors.White);
      }, cancellationTokenSource.Token);
    }

    private void LoadSettings()
    {
      this.options.Value.Tokens
        .Select(x => new SlackToken(x))
        .ForEach(this.Tokens.Add);

      this.ShowUnreadMentions = this.options.Value.ShowUnreadMentions;
      this.ShowUnreadMessages = this.options.Value.ShowUnreadMessages;
      this.ShowStatus = this.options.Value.ShowStatus;
      this.BrightnessPercent = this.options.Value.Brightness;
    }

    private void SaveSettings()
    {
      this.options.Update(x =>
      {
        x.Tokens = this.Tokens.Select(slackToken => slackToken.Token).ToArray();
        x.ShowUnreadMentions = this.ShowUnreadMentions;
        x.ShowUnreadMessages = this.ShowUnreadMessages;
        x.ShowStatus = this.ShowStatus;
        x.Brightness = this.BrightnessPercent;
      });
    }

    private void AddToken()
    {
      var tokenToAdd = this.NewToken.Trim();
      if (!string.IsNullOrEmpty(tokenToAdd))
      {
        this.Tokens.Add(new SlackToken(tokenToAdd));
        this.NewToken = string.Empty;
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
