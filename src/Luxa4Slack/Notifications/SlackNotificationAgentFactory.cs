namespace CG.Luxa4Slack.Notifications
{
  using System;
  using CG.Luxa4Slack.Abstractions;
  using Microsoft.Extensions.Logging;
  using SlackAPI;

  internal class SlackNotificationAgentFactory : ISlackNotificationAgentFactory
  {
    private readonly Func<string, SlackSocketClient> _slackSocketClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public SlackNotificationAgentFactory(Func<string, SlackSocketClient> slackSocketClientFactory, ILoggerFactory loggerFactory)
    {
      _slackSocketClientFactory = slackSocketClientFactory;
      _loggerFactory = loggerFactory;
    }

    public ISlackNotificationAgent Create(string token)
    {
      return new SlackNotificationAgent(token, _slackSocketClientFactory, _loggerFactory);
    }
  }
}
