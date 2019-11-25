namespace CG.Luxa4Slack
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Threading;

  internal class ChannelsInfo : IEnumerable<KeyValuePair<string, ChannelInfo>>
  {
    private readonly Dictionary<string, ChannelInfo> items = new Dictionary<string, ChannelInfo>();
    private readonly object locker = new object();

    public event Action Changed;

    public ChannelInfo this[string channelId]
    {
      get
      {
        lock (this.locker)
        {
          if (this.items.TryGetValue(channelId, out var channelNotification) == false)
          {
            channelNotification = new ChannelInfo();
            channelNotification.Changed += this.OnChanged;
            this.items[channelId] = channelNotification;
          }

          return channelNotification;
        }
      }
    }

    public void Clear()
    {
      lock (this.locker)
      {
        this.items.Clear();
      }
    }

    public IEnumerator<KeyValuePair<string, ChannelInfo>> GetEnumerator()
    {
      return new SafeEnumerator<KeyValuePair<string, ChannelInfo>>(this.items, this.locker);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }

    private void OnChanged()
    {
      this.Changed?.Invoke();
    }

    private class SafeEnumerator<T> : IEnumerator<T>
    {
      private readonly IEnumerator<T> enumerator;
      private readonly object locker;

      public SafeEnumerator(IEnumerable<T> enumerable, object locker)
      {
        this.locker = locker;

        Monitor.Enter(this.locker);
        this.enumerator = enumerable.GetEnumerator();
      }

      public T Current
      {
        get
        {
          return this.enumerator.Current;
        }
      }

      object IEnumerator.Current
      {
        get
        {
          return this.Current;
        }
      }

      public void Dispose()
      {
        this.enumerator.Dispose();
        Monitor.Exit(this.locker);
      }

      public bool MoveNext()
      {
        return this.enumerator.MoveNext();
      }

      public void Reset()
      {
        this.enumerator.Reset();
      }
    }
  }
}
