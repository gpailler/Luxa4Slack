namespace CG.Luxa4Slack
{
  using System;

  public static class OAuthInfo
  {
    public const string RedirectedUri = "https://luxa4slack-oauth.azurewebsites.net/api/Redirect";

    public const string ClientId = "707831884096.696442695363";

    public static readonly string SecretId = Environment.GetEnvironmentVariable("LUXA4SLACK_SECRETID") ?? throw new Exception($"'LUXA4SLACK_SECRETID' environment variable is not defined");

    public static readonly Uri SlackOAuthUri = new Uri("https://slack.com/api/oauth.access");
  }
}
