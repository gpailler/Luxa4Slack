namespace CG.Luxa4Slack.Notifications
{
  using System;
  using CG.Luxa4Slack.Abstractions;
  using Microsoft.Extensions.Logging;
  using SlackAPI;

  internal class SlackNotificationResilientAgentFactory : ISlackNotificationAgentFactory
  {
    private readonly Func<string, SlackSocketClient> _slackSocketClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public SlackNotificationResilientAgentFactory(Func<string, SlackSocketClient> slackSocketClientFactory, ILoggerFactory loggerFactory)
    {
      _slackSocketClientFactory = slackSocketClientFactory;
      _loggerFactory = loggerFactory;
    }

    public ISlackNotificationAgent Create(string token)
    {
      return new SlackNotificationResilientAgent(token, _slackSocketClientFactory, _loggerFactory);
    }
  }
}
