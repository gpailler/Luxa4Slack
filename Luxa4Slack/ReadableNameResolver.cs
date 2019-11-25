using System.Linq;

namespace CG.Luxa4Slack
{
  using SlackAPI;

  internal class ReadableNameResolver
  {
    private readonly SlackSocketClient client;

    public ReadableNameResolver(SlackSocketClient client)
    {
      this.client = client;
    }

    public string Resolve(string id)
    {
      var user = this.client.Users.FirstOrDefault(x => x.id == id);
      if (user != null)
      {
        return string.IsNullOrEmpty(user.profile.real_name) ? user.name : user.profile.real_name;
      }

      var im = this.client.DirectMessages.FirstOrDefault(x => x.id == id);
      if (im != null)
      {
        return this.Resolve(im.user);
      }

      var channel = this.client.Channels.Union(this.client.Groups).FirstOrDefault(x => x.id == id);
      if (channel != null)
      {
        return channel.name;
      }

      return $"[No readable name found for '{id}'";
    }
  }
}
