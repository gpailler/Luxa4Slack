namespace CG.Luxa4Slack
{
  using System;

  using SlackAPI;

  public class OAuthHelper
  {
    public static Uri GetAuthorizationUri()
    {
      var slackClientHelper = new SlackClientHelpers();
      return slackClientHelper.GetAuthorizeUri(OAuthInfo.ClientId, SlackScope.Client, OAuthInfo.RedirectedUri);
    }
  }
}
