namespace CG.Luxa4Slack.Tray
{
  using System;
  using CG.Luxa4Slack.Tray.Options;
  using Microsoft.Extensions.Configuration;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Logging;
  using NLog;
  using NLog.Extensions.Logging;

  internal static class Program
  {
    [STAThread]
    private static void Main()
    {
      var provider = ConfigureServices()
        .BuildServiceProvider(validateScopes: true);

      using var scope = provider.CreateScope();
      scope.ServiceProvider.GetRequiredService<ApplicationStartup>().Run();
      provider.Dispose();
    }

    private static IServiceCollection ConfigureServices()
    {
      var serviceCollection = new ServiceCollection();

      var configuration = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.logging.json", optional: false, reloadOnChange: false)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

      serviceCollection.AddSingleton(configuration);
      serviceCollection.ConfigureWritable<ApplicationOptions>(configuration.GetSection(ApplicationOptions.Key));

      serviceCollection.AddLogging(loggingBuilder =>
      {
        loggingBuilder.AddNLog(configuration);
        loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);

        // Read NLog configuration from appsettings.json
        LogManager.Configuration = new NLogLoggingConfiguration(configuration.GetSection("NLog"));
      });

      serviceCollection.RegisterLuxa4SlackTray();
      serviceCollection.RegisterLuxa4Slack();

      return serviceCollection;
    }
  }
}
