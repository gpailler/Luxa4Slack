namespace CG.Luxa4Slack
{
  using System;

  public static class OAuthInfo
  {
    public const string RedirectedUri = "https://luxa4slack.awan.tech/OAuth";

    public const string ClientId = "3120088010.52664063651";

    public static readonly string SecretId = Environment.GetEnvironmentVariable("LUXA4SLACK_SECRETID");

    public static readonly Uri SlackOAuthUri = new Uri("https://slack.com/api/oauth.access");
  }
}