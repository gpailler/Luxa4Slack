namespace CG.Luxa4Slack.NotificationClient
{
  public class NotificationClientFactory
  {
    public static INotificationClient Create(double brightness, bool useLoggerClient)
    {
      return useLoggerClient
        ? (INotificationClient)new LoggerClient(brightness)
        : new LuxaforClient(brightness);
    }
  }
}
