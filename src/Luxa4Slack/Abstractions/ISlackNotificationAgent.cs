namespace CG.Luxa4Slack.Abstractions
{
  using System;
  using System.Threading.Tasks;

  internal interface ISlackNotificationAgent : IDisposable
  {
    event Action Changed;

    Task<bool> InitializeAsync();

    bool HasUnreadMessages { get; }

    bool HasUnreadMentions { get; }

    bool IsAway { get; }
  }
}
