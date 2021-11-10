namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using CG.Luxa4Slack.Extensions;
  using Microsoft.Extensions.Logging;
  using SlackAPI;

  internal class HandlerContext
  {
    private static readonly TimeSpan s_refreshDelay = TimeSpan.FromHours(1);

    private readonly SlackSocketClient _client;
    private readonly ReadableNameResolver _readableNameResolver;
    private readonly ILogger _logger;
    private readonly ManualResetEventSlim _waiter = new(false);
    private readonly Timer _timer;

    private HashSet<string> _highlightWords = null!;
    private HashSet<string> _mutedChannels = null!;

    public HandlerContext(SlackSocketClient client, ChannelsInfo channelsInfo, ReadableNameResolver readableNameResolver, ILogger logger)
    {
      _client = client;
      ChannelsInfo = channelsInfo;
      _readableNameResolver = readableNameResolver;
      _logger = logger;

      _timer = new Timer(_ =>
      {
        if (!_client.IsConnected)
        {
          _timer!.Dispose();
        }

        Refresh();
      }, null, TimeSpan.Zero, s_refreshDelay);
    }

    public ChannelsInfo ChannelsInfo { get; }

    public HashSet<string> HighlightWords
    {
      get
      {
        _waiter.Wait();
        return _highlightWords;
      }
      private set => _highlightWords = value;
    }

    public HashSet<string> MutedChannels
    {
      get
      {
        _waiter.Wait();
        return _mutedChannels;
      }
      private set => _mutedChannels = value;
    }

    public string GetNameFromId(string id)
    {
      return _readableNameResolver.Resolve(id);
    }

    private void Refresh()
    {
      _waiter.Reset();

      _client.GetPreferences(response =>
      {
        HighlightWords = GetHighlightWords(response.prefs.highlight_words);
        MutedChannels = GetMutedChannels(response.prefs.muted_channels);
        _waiter.Set();
      });

      _waiter.Wait(SlackNotificationAgent.Timeout);

      _logger.LogDebug("Highlight words for mention detection: {0}", string.Join(", ", HighlightWords));
      _logger.LogDebug("Muted channels: {0}", string.Join(", ", MutedChannels));
    }

    private HashSet<string> GetHighlightWords(string input)
    {
      // Add some keywords for mention detection
      var highlightWords = new HashSet<string>
      {
        "<!channel>",
        _client.MySelf.id,
        _client.MySelf.name
      };

      input
        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .ForEach(x => highlightWords.Add(x));

      return highlightWords;
    }

    private static HashSet<string> GetMutedChannels(string input)
    {
      var result = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim());

      return new HashSet<string>(result);
    }
  }
}
