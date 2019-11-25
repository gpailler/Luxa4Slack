namespace Luxa4Slack.OAuth.AzureFunctions
{
  using System;
  using System.Collections.Generic;
  using System.Net.Http;
  using CG.Luxa4Slack;

  using System.Threading.Tasks;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.Azure.WebJobs;
  using Microsoft.Azure.WebJobs.Extensions.Http;
  using Microsoft.AspNetCore.Http;
  using Microsoft.Extensions.Logging;
  using Newtonsoft.Json;

  public class RedirectFunction
  {
    private static readonly HttpClient HttpClient = new HttpClient();

    public RedirectFunction()
    {
      // Ensure SecretId is defined
      GC.KeepAlive(OAuthInfo.SecretId); 
    }

    [FunctionName("Redirect")]
    public async Task<IActionResult> Run(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
      HttpRequest request,
      ILogger log)
    {
      log.LogInformation("Redirect HTTP trigger function processed a request.");

      string code = request.Query["code"];
      if (string.IsNullOrEmpty(code))
      {
        return new BadRequestObjectResult("'code' not found in the query string");
      }

      var getTokenUri = this.BuildGetTokenUri(OAuthInfo.ClientId, OAuthInfo.SecretId, OAuthInfo.RedirectedUri, code);
      var response = await HttpClient.GetAsync(getTokenUri);
      if (response.IsSuccessStatusCode)
      {
        string result = await response.Content.ReadAsStringAsync();
        var resultObjectTemplate = new {ok = false, access_token = string.Empty, error = string.Empty};
        var resultObject = JsonConvert.DeserializeAnonymousType(result, resultObjectTemplate);

        if (resultObject.ok)
        {
          return new OkObjectResult($"You Slack token is: {resultObject.access_token}");
        }
        else
        {
          log.LogError($"Unable to retrieve Slack token. Code: {code} / Error: {resultObject.error}");
          return new BadRequestObjectResult($"Unable to retrieve Slack token. Error: {resultObject.error}");
        }
      }
      else
      {
        log.LogError($"Unable to retrieve Slack token. Code: {response.StatusCode} / Error: {response.ReasonPhrase}");
        return new BadRequestObjectResult($"Unable to retrieve Slack token. Error: {response.ReasonPhrase}");
      }
    }

    private Uri BuildGetTokenUri(string clientId, string clientSecret, string redirectUri, string code)
    {
      var query = new FormUrlEncodedContent(new[]
      {
        new KeyValuePair<string, string>("client_id", clientId),
        new KeyValuePair<string, string>("client_secret", clientSecret),
        new KeyValuePair<string, string>("code", code),
        new KeyValuePair<string, string>("redirect_uri", redirectUri)
      });

      var uriBuilder = new UriBuilder(OAuthInfo.SlackOAuthUri);
      uriBuilder.Query = query.ReadAsStringAsync().Result;

      return uriBuilder.Uri;
    }
  }
}

