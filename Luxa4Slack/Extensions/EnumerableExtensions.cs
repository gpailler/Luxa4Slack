namespace CG.Luxa4Slack.Extensions
{
  using System;
  using System.Collections.Generic;

  public static class EnumerableExtensions
  {
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
      foreach (var item in collection)
      {
        action(item);
      }
    }
  }
}
