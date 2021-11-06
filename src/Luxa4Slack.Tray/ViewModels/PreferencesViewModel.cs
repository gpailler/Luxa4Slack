namespace CG.Luxa4Slack.Tray.ViewModels
{
  using System.Collections.ObjectModel;
  using System.Diagnostics;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows.Input;
  using CG.Luxa4Slack.Abstractions;
  using CG.Luxa4Slack.Extensions;
  using CG.Luxa4Slack.Tray.Options;
  using Microsoft.Toolkit.Mvvm.ComponentModel;
  using Microsoft.Toolkit.Mvvm.Input;
  using Microsoft.Toolkit.Mvvm.Messaging;

  public class PreferencesViewModel : ObservableRecipient
  {
    private readonly IWritableOptions<ApplicationOptions> _options;
    private readonly ILuxaforClient _luxaforClient;

    private int _brightness;
    private string _newToken = string.Empty;
    private CancellationTokenSource? _cancellationTokenSource;

    public PreferencesViewModel(IWritableOptions<ApplicationOptions> options, ApplicationInfo applicationInfo, IMessenger messenger, ILuxaforClient luxaforClient)
      : base(messenger)
    {
      _options = options;
      Title = applicationInfo.Format("Preferences");
      Tokens = new ObservableCollection<SlackToken>();

      UpdatePreferencesCommand = new RelayCommand(() =>
      {
        SaveSettings();
        CloseCommand?.Execute(null);
      });
      CloseCommand = new RelayCommand(() => Messenger.Send(new CloseWindowMessage()));
      RequestTokenCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo(OAuthHelper.GetAuthorizationUri().ToString()) { UseShellExecute = true }));
      RemoveTokenCommand = new RelayCommand<SlackToken>(x => Tokens.Remove(x!));
      AddTokenCommand = new RelayCommand(() => AddToken());

      _luxaforClient = luxaforClient;

      LoadSettings();
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
      get => _brightness;
      set
      {
        if (SetProperty(ref _brightness, value))
        {
          OnPropertyChanged(nameof(BrightnessPercent));
          UpdateLuxafor(BrightnessPercent);
        }
      }
    }

    public double BrightnessPercent
    {
      get => Brightness / 100d;
      set => Brightness = (int)(value * 100d);
    }

    public string NewToken
    {
      get => _newToken;
      set => SetProperty(ref _newToken, value);
    }

    private void UpdateLuxafor(double brightnessPercent)
    {
      // Cancel pending updates
      _cancellationTokenSource?.Cancel();
      _cancellationTokenSource = new CancellationTokenSource();

      Task.Run(async () =>
      {
        _luxaforClient.SetBrightness(brightnessPercent);
        await _luxaforClient.SetAsync(LuxaforColor.White);
      }, _cancellationTokenSource.Token);
    }

    private void LoadSettings()
    {
      _options.Value.Tokens
        .Select(x => new SlackToken(x))
        .ForEach(Tokens.Add);

      ShowUnreadMentions = _options.Value.ShowUnreadMentions;
      ShowUnreadMessages = _options.Value.ShowUnreadMessages;
      ShowStatus = _options.Value.ShowStatus;
      BrightnessPercent = _options.Value.Brightness;
    }

    private void SaveSettings()
    {
      _options.Update(x =>
      {
        x.Tokens = Tokens.Select(slackToken => slackToken.Token).ToArray();
        x.ShowUnreadMentions = ShowUnreadMentions;
        x.ShowUnreadMessages = ShowUnreadMessages;
        x.ShowStatus = ShowStatus;
        x.Brightness = BrightnessPercent;
      });
    }

    private void AddToken()
    {
      var tokenToAdd = NewToken.Trim();
      if (!string.IsNullOrEmpty(tokenToAdd))
      {
        Tokens.Add(new SlackToken(tokenToAdd));
        NewToken = string.Empty;
      }
    }

    public class SlackToken : ObservableObject
    {
      public SlackToken(string token)
      {
        Token = token;
        Workspace = "Loading...";

        Task.Run(() =>
        {
          Workspace = WorkspaceHelper.GetWorkspace(token);
          OnPropertyChanged(nameof(Workspace));
        });
      }

      public string Token { get; }

      public string Workspace { get; private set; }
    }
  }
}
