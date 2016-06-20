namespace CG.Luxa4Slack.Console
{
  using System;

  using CommandLine;

  using NLog;

  class Program
  {
    private static readonly ILogger logger = LogManager.GetLogger("Luxa4Slack.Console");
    private static Luxa4Slack luxa4Slack;
    private static CommandLineOptions commandLineOptions;

    static void Main(string[] args)
    {
      commandLineOptions = ParseCommandLine(args);
      if (commandLineOptions != null)
      {
        luxa4Slack = new Luxa4Slack(commandLineOptions.Token, commandLineOptions.ShowUnreadMentions, commandLineOptions.ShowUnreadMessages);
        try
        {
          luxa4Slack.Initialize();
          luxa4Slack.LuxaforFailure += OnLuxaforFailure;
        }
        catch (Exception ex)
        {
          logger.Error(ex);
        }
      }

      Console.ReadLine();

      luxa4Slack?.Dispose();
    }

    private static CommandLineOptions ParseCommandLine(string[] args)
    {
      var parser = Parser.Default.ParseArguments<CommandLineOptions>(args);
      var result = parser.MapResult(ParseCommandLineResults, x => null);

      return result;
    }

    private static CommandLineOptions ParseCommandLineResults(CommandLineOptions options)
    {
      if (options.Debug)
      {
        foreach (var rule in LogManager.Configuration.LoggingRules)
        {
          rule.EnableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
        }

        LogManager.ReconfigExistingLoggers();
      }

      return options;
    }

    private static void OnLuxaforFailure()
    {
      logger.Error("Luxafor communication issue. Please unplug/replug the Luxafor and restart the application");
    }
  }
}
