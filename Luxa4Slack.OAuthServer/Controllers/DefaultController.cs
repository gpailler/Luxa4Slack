namespace CG.Luxa4Slack.OAuthServer.Controllers
{
  using System;
  using System.Collections.Generic;
  using System.Net.Http;
  using System.Reflection;
  using Microsoft.AspNetCore.Mvc;

  using Newtonsoft.Json;

  public class DefaultController : Controller
  {
    public IActionResult Index()
    {
      this.ViewData["Version"] = this.GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
      return this.View();
    }

    public IActionResult OAuth(string code)
    {
      if (string.IsNullOrEmpty(code))
      {
        throw new ArgumentException("Invalid parameters");
      }

      using (var client = new HttpClient())
      {
        var getTokenUri = this.BuildGetTokenUri(OAuthInfo.ClientId, OAuthInfo.SecretId, OAuthInfo.RedirectedUri, code);
        var response = client.GetAsync(getTokenUri);
        if (response.Result.IsSuccessStatusCode)
        {
          string result = response.Result.Content.ReadAsStringAsync().Result;
          var resultObjectTemplate = new { ok = false, access_token = string.Empty, error = string.Empty };
          var resultObject = JsonConvert.DeserializeAnonymousType(result, resultObjectTemplate);

          if (resultObject.ok)
          {
            this.ViewData["Message"] = $"You Slack token is: {resultObject.access_token}";
          }
          else
          {
            this.ViewData["Message"] = $"Unable to retrieve Slack token. Error: {resultObject.error}";
          }
        }
        else
        {
          throw new Exception(response.Result.ReasonPhrase);
        }
      }

      return this.View();
    }

    public IActionResult Error()
    {
      return this.View();
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
