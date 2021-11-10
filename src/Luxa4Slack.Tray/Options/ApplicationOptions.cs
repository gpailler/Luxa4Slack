namespace CG.Luxa4Slack.Tray.Options
{
  using System;

  public class ApplicationOptions
  {
    public const string Key = nameof(ApplicationOptions);

    public string[] Tokens { get; set; } = Array.Empty<string>();

    public bool ShowStatus { get; set; }

    public bool ShowUnreadMessages { get; set; } = true;

    public bool ShowUnreadMentions { get; set; } = true;

    public double Brightness { get; set; } = 0.5;
  }
}
