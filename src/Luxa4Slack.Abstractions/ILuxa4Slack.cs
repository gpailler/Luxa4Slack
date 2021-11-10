namespace CG.Luxa4Slack.Abstractions
{
  using System;
  using System.Threading.Tasks;

  public interface ILuxa4Slack : IDisposable
  {
    Task InitializeAsync();
  }
}
