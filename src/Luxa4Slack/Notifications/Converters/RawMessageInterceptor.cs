namespace CG.Luxa4Slack.Notifications.Converters
{
  using System.Reflection;
  using Castle.DynamicProxy;

  internal class RawMessageInterceptor : IInterceptor
  {
    private static readonly MethodInfo s_getDataProperty = typeof(IRawMessage).GetProperty(nameof(IRawMessage.Data))!.GetGetMethod()!;
    private readonly string _raw;

    public RawMessageInterceptor(string raw)
    {
      _raw = raw;
    }

    public void Intercept(IInvocation invocation)
    {
      if (invocation.Method == s_getDataProperty)
      {
        invocation.ReturnValue = _raw;
      }
      else
      {
        invocation.Proceed();
      }
    }
  }
}
