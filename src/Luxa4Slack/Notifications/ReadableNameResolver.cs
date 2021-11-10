using System.Linq;

namespace CG.Luxa4Slack.Notifications
{
  using SlackAPI;

  internal class ReadableNameResolver
  {
    private readonly SlackSocketClient _client;

    public ReadableNameResolver(SlackSocketClient client)
    {
      _client = client;
    }

    public string Resolve(string id)
    {
      var user = _client.Users.FirstOrDefault(x => x.id == id);
      if (user != null)
      {
        return string.IsNullOrEmpty(user.profile.real_name) ? user.name : user.profile.real_name;
      }

      var im = _client.DirectMessages.FirstOrDefault(x => x.id == id);
      if (im != null)
      {
        return Resolve(im.user);
      }

      var channel = _client.Channels.Union(_client.Groups).FirstOrDefault(x => x.id == id);
      if (channel != null)
      {
        return channel.name;
      }

      return $"No readable name found for '{id}'";
    }
  }
}
