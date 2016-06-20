namespace CG.Luxa4Slack.Console
{
  using CommandLine;

  public class CommandLineOptions
  {
    [Option('t', "token", Required = true, HelpText = "Slack token (https://api.slack.com/docs/oauth-test-tokens)")]
    public string Token { get; set; }

    [Option('m', "showUnreadMessages", Default = true, HelpText = "Show unread messages")]
    public bool ShowUnreadMessages { get; set; }

    [Option('M', "showUnreadMentions", Default = true, HelpText = "Show unread mentions")]
    public bool ShowUnreadMentions { get; set; }

    [Option('d', "debug", Default = false, HelpText = "Show debug informations")]
    public bool Debug { get; set; }
  }
}
