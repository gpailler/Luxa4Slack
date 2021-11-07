namespace CG.Luxa4Slack.Abstractions
{
  using System;
  using System.Threading.Tasks;

  public interface IMessageHandler : IDisposable
  {
    Task InitializeAsync();
  }
}
