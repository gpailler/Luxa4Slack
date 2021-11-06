namespace CG.Luxa4Slack.Tray
{
  using System.Threading;
  using SlackAPI;

  internal static class WorkspaceHelper
  {
    private const int Timeout = 10000;

    public static string GetWorkspace(string token)
    {
      using var connectionEvent = new ManualResetEventSlim(false);
      var client = new SlackClient(token);

      LoginResponse? loginResponse = null;
      client.Connect(response =>
      {
        loginResponse = response;
        // ReSharper disable once AccessToDisposedClosure
        connectionEvent.Set();
      });
      connectionEvent.Wait(Timeout);

      return loginResponse?.ok == true
        ? loginResponse.team.name
        : loginResponse?.error ?? "Empty response";
    }
  }
}
