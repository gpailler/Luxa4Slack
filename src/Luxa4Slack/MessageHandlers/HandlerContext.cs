namespace CG.Luxa4Slack.MessageHandlers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using NLog;
  using SlackAPI;
  using Theraot.Collections;

  internal class HandlerContext
  {
    private static readonly TimeSpan RefreshDelay = TimeSpan.FromHours(1);

    private readonly ILogger Logger = LogManager.GetLogger(nameof(HandlerContext));
    private readonly SlackSocketClient client;
    private readonly ReadableNameResolver readableNameResolver;
    private readonly ManualResetEventSlim waiter = new ManualResetEventSlim(false);

    private HashSet<string> highlightWords;
    private HashSet<string> mutedChannels;
    private Timer timer;

    public HandlerContext(SlackSocketClient client, ChannelsInfo channelsInfo, ReadableNameResolver readableNameResolver)
    {
      this.client = client;
      this.ChannelsInfo = channelsInfo;
      this.readableNameResolver = readableNameResolver;

      this.timer = new Timer(x =>
      {
        if (!this.client.IsConnected)
        {
          this.timer.Dispose();
        }

        this.Refresh();
      }, null, TimeSpan.Zero, RefreshDelay);
    }

    public ChannelsInfo ChannelsInfo { get; }

    public HashSet<string> HighlightWords
    {
      get
      {
        this.waiter.Wait();
        return this.highlightWords;
      }
      private set
      {
        this.highlightWords = value;
      }
    }

    public HashSet<string> MutedChannels
    {
      get
      {
        this.waiter.Wait();
        return this.mutedChannels;
      }
      private set
      {
        this.mutedChannels = value;
      }
    }

    public string GetNameFromId(string id)
    {
      return this.readableNameResolver.Resolve(id);
    }

    private void Refresh()
    {
      this.waiter.Reset();

      this.client.GetPreferences(response =>
      {
        this.HighlightWords = this.GetHighlightWords(response.prefs.highlight_words);
        this.MutedChannels = this.GetMutedChannels(response.prefs.muted_channels);
        this.waiter.Set();
      });

      this.waiter.Wait(SlackNotificationAgent.Timeout);

      this.Logger.Debug("Highlight words for mention detection: {0}", string.Join(", ", this.HighlightWords));
      this.Logger.Debug("Muted channels: {0}", string.Join(", ", this.MutedChannels));
    }

    private HashSet<string> GetHighlightWords(string input)
    {
      // Add some keywords for mention detection
      var highlightWords = new HashSet<string>();
      highlightWords.Add("<!channel>");
      highlightWords.Add(this.client.MySelf.id);
      highlightWords.Add(this.client.MySelf.name);

      highlightWords.AddRange(input
        .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim()));

      return highlightWords;
    }

    private HashSet<string> GetMutedChannels(string input)
    {
      var result = input.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim());

      return new HashSet<string>(result);
    }
  }
}
