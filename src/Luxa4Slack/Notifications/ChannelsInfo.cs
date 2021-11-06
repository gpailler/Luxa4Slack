namespace CG.Luxa4Slack.Notifications
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Threading;

  internal class ChannelsInfo : IEnumerable<KeyValuePair<string, ChannelInfo>>
  {
    private readonly Dictionary<string, ChannelInfo> _items = new();
    private readonly object _locker = new();

    public event Action? Changed;

    public ChannelInfo this[string channelId]
    {
      get
      {
        lock (_locker)
        {
          if (_items.TryGetValue(channelId, out var channelNotification) == false)
          {
            channelNotification = new ChannelInfo();
            channelNotification.Changed += OnChanged;
            _items[channelId] = channelNotification;
          }

          return channelNotification;
        }
      }
    }

    public IEnumerator<KeyValuePair<string, ChannelInfo>> GetEnumerator()
    {
      return new SafeEnumerator<KeyValuePair<string, ChannelInfo>>(_items, _locker);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    private void OnChanged()
    {
      Changed?.Invoke();
    }

    private class SafeEnumerator<T> : IEnumerator<T>
    {
      private readonly IEnumerator<T> _enumerator;
      private readonly object _locker;

      public SafeEnumerator(IEnumerable<T> enumerable, object locker)
      {
        _locker = locker;

        Monitor.Enter(_locker);
        _enumerator = enumerable.GetEnumerator();
      }

      public T Current => _enumerator.Current;

      object? IEnumerator.Current => Current;

      public void Dispose()
      {
        _enumerator.Dispose();
        Monitor.Exit(_locker);
      }

      public bool MoveNext()
      {
        return _enumerator.MoveNext();
      }

      public void Reset()
      {
        _enumerator.Reset();
      }
    }
  }
}
