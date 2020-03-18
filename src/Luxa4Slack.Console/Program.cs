﻿namespace CG.Luxa4Slack.Console
{
  using System;
  using System.Diagnostics;
  using System.Threading.Tasks;
  using CG.Luxa4Slack.NotificationClient;
  using CommandLine;

  using NLog;

  public class Program
  {
    private static readonly ILogger logger = LogManager.GetLogger("Luxa4Slack.Console");
    private static Luxa4Slack luxa4Slack;
    private static CommandLineOptions commandLineOptions;

    public static async Task Main(string[] args)
    {
      commandLineOptions = ParseCommandLine(args);
      if (commandLineOptions != null)
      {
        if (commandLineOptions.RequestToken)
        {
          logger.Warn("Please visit following uri to retrieve your Slack token");
          logger.Warn(OAuthHelper.GetAuthorizationUri());
        }
        else
        {
          luxa4Slack = new Luxa4Slack(
            commandLineOptions.Tokens,
            commandLineOptions.ShowUnreadMentions,
            commandLineOptions.ShowUnreadMessages,
            commandLineOptions.ShowStatus,
            () => NotificationClientFactory.Create(commandLineOptions.Brightness, commandLineOptions.Debug || Debugger.IsAttached));

          try
          {
            await luxa4Slack.Initialize();
            luxa4Slack.NotificationClientFailure += OnNotificationClientFailure;

            Console.ReadLine();
          }
          catch (Exception ex)
          {
            logger.Error(ex);
          }
          finally
          {
            luxa4Slack.Dispose();
          }
        }
      }
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

    private static void OnNotificationClientFailure()
    {
      logger.Error("Luxafor communication issue. Please unplug/replug the Luxafor and restart the application");
    }
  }
}
