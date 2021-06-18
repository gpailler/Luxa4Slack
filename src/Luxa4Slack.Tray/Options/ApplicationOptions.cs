namespace CG.Luxa4Slack.Tray.Options
{
  public class ApplicationOptions
  {
    public const string Key = nameof(ApplicationOptions);

    public string[] Tokens { get; set; } = new string[0];

    public bool ShowStatus { get; set; } = false;

    public bool ShowUnreadMessages { get; set; } = true;

    public bool ShowUnreadMentions { get; set; } = true;

    public double Brightness { get; set; } = 0.5;
  }
}
