namespace CG.Luxa4Slack.Abstractions
{
  using System.Collections.Generic;

  public interface ILuxa4SlackFactory
  {
    ILuxa4Slack Create(IEnumerable<string> slackTokens, bool showUnreadMentions, bool showUnreadMessages, bool showStatus, double brightness);
  }
}
