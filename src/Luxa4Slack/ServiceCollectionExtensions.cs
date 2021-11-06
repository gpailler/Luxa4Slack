namespace CG.Luxa4Slack
{
  using System;
  using CG.Luxa4Slack.Abstractions;
  using CG.Luxa4Slack.Abstractions.Luxafor;
  using CG.Luxa4Slack.Luxafor;
  using CG.Luxa4Slack.Notifications;
  using Microsoft.Extensions.DependencyInjection;
  using SlackAPI;

  public static class ServiceCollectionExtensions
  {
    public static void RegisterLuxa4Slack(this IServiceCollection serviceCollection)
    {
      serviceCollection.AddSingleton<ILuxaforClient, LuxaforClient>();
      serviceCollection.AddSingleton<ILuxa4SlackFactory, Luxa4SlackFactory>();
      serviceCollection.AddSingleton<ISlackNotificationAgentFactory, SlackNotificationResilientAgentFactory>();
      serviceCollection.AddSingleton<Func<string, SlackSocketClient>>(x => new SlackSocketClient(x));
    }
  }
}
