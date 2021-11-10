namespace Luxa4Slack.OAuth.AzureFunctions
{
  using System.Diagnostics;
  using Microsoft.AspNetCore.Http;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.Azure.WebJobs;
  using Microsoft.Azure.WebJobs.Extensions.Http;
  using Microsoft.Extensions.Logging;

  public class VersionFunction
  {
    [FunctionName("Version")]
    public IActionResult Run(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
      HttpRequest request,
      ILogger log)
    {
      log.LogInformation("'Version' HTTP trigger function processed a request.");

      var assembly = GetType().Assembly;
      return new OkObjectResult($"{assembly.GetName().Name} - {FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).ProductVersion}");
    }
  }
}

