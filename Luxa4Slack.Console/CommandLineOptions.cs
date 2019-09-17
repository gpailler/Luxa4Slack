namespace CG.Luxa4Slack.Console
{
  using CommandLine;

  public class CommandLineOptions
  {
    [Option('t', "token", Required = true, SetName = "run", HelpText = "Slack token (use --requesttoken option)")]
    public string Token { get; set; }

    [Option('m', "showUnreadMessages", Default = false, SetName = "run", HelpText = "Show unread messages")]
    public bool ShowUnreadMessages { get; set; }

    [Option('M', "showUnreadMentions", Default = false, SetName = "run", HelpText = "Show unread mentions")]
    public bool ShowUnreadMentions { get; set; }

    [Option('s', "showStatus", Default = true, SetName = "run", HelpText = "Show status changes")]
    public bool ShowStatus { get; set; }

    [Option('d', "debug", Default = false, SetName = "run", HelpText = "Show debug informations")]
    public bool Debug { get; set; }

    [Option("requesttoken", Default = false, Required = true, SetName = "request", HelpText = "Request a Slack token")]
    public bool RequestToken { get; set; }
  }
}
