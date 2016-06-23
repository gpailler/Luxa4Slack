namespace CG.Luxa4Slack
{
  using System;

  using SlackAPI;

  public class OAuthHelper
  {
    public static Uri GetAuthorizationUri()
    {
      return SlackClient.GetAuthorizeUri(OAuthInfo.ClientId, SlackScope.Client, OAuthInfo.RedirectedUri);
    }
  }
}
