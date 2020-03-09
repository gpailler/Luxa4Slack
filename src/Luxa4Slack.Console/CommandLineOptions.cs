namespace CG.Luxa4Slack.Console
{
  using System.Collections.Generic;
  using CommandLine;

  public class CommandLineOptions
  {
    [Option('t', "tokens", Required = true, SetName = "run", HelpText = "Slack token (use --requesttoken option)")]
    public IEnumerable<string> Tokens { get; set; }

    [Option('m', "showUnreadMessages", Default = true, SetName = "run", HelpText = "Show unread messages")]
    public bool ShowUnreadMessages { get; set; }

    [Option('M', "showUnreadMentions", Default = true, SetName = "run", HelpText = "Show unread mentions")]
    public bool ShowUnreadMentions { get; set; }

    [Option('s', "showStatus", Default = false, SetName = "run", HelpText = "Show status changes")]
    public bool ShowStatus { get; set; }

    [Option('b', "brightness", Default = true, SetName = "run", HelpText = "Luxafor brightness")]
    public double Brightness { get; set; }

    [Option('d', "debug", Default = false, SetName = "run", HelpText = "Show debug informations")]
    public bool Debug { get; set; }

    [Option("requesttoken", Default = false, Required = true, SetName = "request", HelpText = "Request a Slack token")]
    public bool RequestToken { get; set; }
  }
}
