namespace CG.Luxa4Slack.Extensions
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;

  public static class WaitHandleExtensions
  {
    public static Task<bool> WaitOneAsync(this WaitHandle waitHandle, int timeout)
    {
      if (waitHandle == null)
      {
        throw new ArgumentNullException(nameof(waitHandle));
      }

      var tcs = new TaskCompletionSource<bool>();

      var registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
        waitHandle,
        callBack: (state, timedOut) => { tcs.TrySetResult(!timedOut); },
        state: null,
        millisecondsTimeOutInterval: timeout,
        executeOnlyOnce: true);

      return tcs.Task.ContinueWith((antecedent) =>
      {
        registeredWaitHandle.Unregister(waitObject: null);
        try
        {
          return antecedent.Result;
        }
        catch
        {
          return false;
        }
      });
    }
  }
}
