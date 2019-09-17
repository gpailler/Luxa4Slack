namespace CG.Luxa4Slack
{

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Net.Http;
  using System.Threading.Tasks;

  using Newtonsoft.Json;


  using NLog;

  public class SlackClient
  {
    private readonly Uri _webhookUrl;
    private readonly HttpClient _httpClient = new HttpClient();
    protected readonly ILogger Logger = LogManager.GetLogger("Slack");

    public SlackClient(Uri webhookUrl)
    {
      _webhookUrl = webhookUrl;
    }

    public async Task<HttpResponseMessage> SendMessageAsync(string message,
        string channel = null, string username = null)
    {
      var payload = new
      {
        text = message,
        channel,
        username,
      };
      var serializedPayload = JsonConvert.SerializeObject(payload);
      var response = await _httpClient.PostAsync(_webhookUrl,
          new StringContent(serializedPayload, Encoding.UTF8, "application/json"));

      return response;
    }

    public async Task<HttpResponseMessage> Presence_Sub(string mytoken)
    {
      var payload = new
      {
        token = mytoken,
        presence_sub = 1,
      };
      var serializedPayload = JsonConvert.SerializeObject(payload);
      this.Logger.Debug($"Subscribe response {serializedPayload}");
      var response = await _httpClient.PostAsync(_webhookUrl,
          new StringContent(serializedPayload, Encoding.UTF8, "application/json"));

      return response;
    }

  }
}
