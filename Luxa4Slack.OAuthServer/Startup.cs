namespace CG.Luxa4Slack.OAuthServer
{
  using Microsoft.AspNetCore.Builder;
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Logging;

  public class Startup
  {
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvc();
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    {
      app.UseExceptionHandler("/Error");

      app.UseMvc(
        routes =>
          {
            routes.MapRoute(
              name: "Default",
              template: "{action}",
              defaults: new { controller = "Default", action = "Index" });
          });
    }
  }
}
