namespace CG.Luxa4Slack
{
  using System.Threading;
  using SlackAPI;

  public class WorkspaceHelper
  {
    public static string GetWorkspace(string token)
    {
      using (var connectionEvent = new ManualResetEventSlim(false))
      {
        var client = new SlackClient(token);

        LoginResponse loginResponse = null;
        client.Connect(response =>
        {
          loginResponse = response;
          connectionEvent.Set();
        });
        connectionEvent.Wait();

        return loginResponse.ok
          ? loginResponse.team.name
          : loginResponse.error;
      }
    }
  }
}
