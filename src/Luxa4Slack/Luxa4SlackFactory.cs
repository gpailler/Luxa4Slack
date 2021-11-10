namespace CG.Luxa4Slack
{
  using System.Collections.Generic;
  using CG.Luxa4Slack.Abstractions;
  using CG.Luxa4Slack.Abstractions.Luxafor;

  internal class Luxa4SlackFactory : ILuxa4SlackFactory
  {
    private readonly ILuxaforClient _luxaforClient;
    private readonly ISlackNotificationAgentFactory _slackNotificationAgentFactory;

    public Luxa4SlackFactory(ILuxaforClient luxaforClient, ISlackNotificationAgentFactory slackNotificationAgentFactory)
    {
      _luxaforClient = luxaforClient;
      _slackNotificationAgentFactory = slackNotificationAgentFactory;
    }

    public ILuxa4Slack Create(IEnumerable<string> slackTokens, bool showUnreadMentions, bool showUnreadMessages, bool showStatus, double brightness)
    {
      return new Luxa4Slack(slackTokens, showUnreadMentions, showUnreadMessages, showStatus, brightness, _luxaforClient, _slackNotificationAgentFactory);
    }
  }
}
