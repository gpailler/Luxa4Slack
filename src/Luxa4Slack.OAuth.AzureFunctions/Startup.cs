using Microsoft.Azure.Functions.Extensions.DependencyInjection;
[assembly: FunctionsStartup(typeof(Luxa4Slack.OAuth.AzureFunctions.Startup))]

namespace Luxa4Slack.OAuth.AzureFunctions
{
  using Microsoft.Extensions.DependencyInjection;

  public class Startup : FunctionsStartup
  {
    public override void Configure(IFunctionsHostBuilder builder)
    {
      builder.Services.AddHttpClient();
    }
  }
}
