namespace CG.Luxa4Slack.Abstractions
{
  internal interface ISlackNotificationAgentFactory
  {
    ISlackNotificationAgent Create(string token);
  }
}
