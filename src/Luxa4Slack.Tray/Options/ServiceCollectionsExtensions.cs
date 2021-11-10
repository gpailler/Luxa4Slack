namespace CG.Luxa4Slack.Tray.Options
{
  using System.IO;
  using Microsoft.Extensions.Configuration;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Options;

  public static class ServiceCollectionExtensions
  {
    public static void ConfigureWritable<T>(
      this IServiceCollection services,
      IConfigurationSection section,
      string file = "appsettings.json") where T : class, new()
    {
      services.Configure<T>(section);
      services.AddTransient<IWritableOptions<T>>(provider =>
      {
        var applicationInfo = provider.GetService<ApplicationInfo>()!;
        var options = provider.GetService<IOptionsMonitor<T>>()!;
        var logger = provider.GetService<ILogger<WritableOptions<T>>>()!;
        return new WritableOptions<T>(options, section.Key, Path.Combine(applicationInfo.ApplicationPath, file), logger);
      });
    }
  }
}
