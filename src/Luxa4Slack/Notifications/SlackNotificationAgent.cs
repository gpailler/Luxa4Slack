namespace CG.Luxa4Slack.Notifications
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using CG.Luxa4Slack.Abstractions;
  using CG.Luxa4Slack.Extensions;
  using CG.Luxa4Slack.Notifications.Converters;
  using CG.Luxa4Slack.Notifications.MessageHandlers;
  using Microsoft.Extensions.Logging;

  using SlackAPI;

  internal class SlackNotificationAgent : ISlackNotificationAgent
  {
    internal const int Timeout = 60000;

    private readonly string _token;
    private readonly Func<string, SlackSocketClient> _slackSocketClientFactory;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    private readonly ChannelsInfo _channelsInfo = new();
    private readonly List<IMessageHandler> _messageHandlers = new();

    private ReadableNameResolver? _readableNameResolver;
    private SlackSocketClient? _client;

    static SlackNotificationAgent()
    {
      var helpers = new SlackClientHelpers();
      helpers.RegisterConverter(new JsonRawConverter());
      helpers.RegisterConverter(new StringToTextConverter());
    }

    public SlackNotificationAgent(string token, Func<string, SlackSocketClient> slackSocketClientFactory, ILoggerFactory loggerFactory)
    {
      _token = token;
      _slackSocketClientFactory = slackSocketClientFactory;
      _loggerFactory = loggerFactory;
      _logger = CreateLogger<SlackNotificationAgent>();
    }

    public event Action? Changed;

    public bool HasUnreadMessages { get; private set; }

    public bool HasUnreadMentions { get; private set; }

    public bool IsAway { get; private set; }

    public async Task<bool> InitializeAsync()
    {
      _logger.LogDebug($"Initialize connection with token: {_token}");

      _client = await ConnectSlackClientAsync();
      if (_client != null)
      {
        _logger.LogDebug($"Connection established");

        _readableNameResolver = new ReadableNameResolver(_client);

        var context = new HandlerContext(_client, _channelsInfo, _readableNameResolver, CreateLogger<HandlerContext>());
        _messageHandlers.Add(new ChannelHandler(_client, context, CreateLogger<ChannelHandler>()));
        _messageHandlers.Add(new GroupHandler(_client, context, CreateLogger<GroupHandler>()));
        _messageHandlers.Add(new ImHandler(_client, context, CreateLogger<ImHandler>()));
        _messageHandlers.Add(new PresenceHandler(_client, OnPresenceChanged, CreateLogger<PresenceHandler>()));

        await Task.WhenAll(_messageHandlers.Select(x => x.InitializeAsync()));
        _channelsInfo.Changed += UpdateStatus;
        UpdateStatus();

        return true;
      }

      return false;
    }

    public void Dispose()
    {
      Changed = null;

      _channelsInfo.Changed -= UpdateStatus;
      _messageHandlers.ForEach(x => x.Dispose());

      _client?.CloseSocket();
    }

    private void UpdateStatus()
    {
      foreach (var channelInfo in _channelsInfo.Where(x => x.Value.HasUnreadMessage || x.Value.HasUnreadMention))
      {
        _logger.LogDebug($"Name: {_readableNameResolver?.Resolve(channelInfo.Key)} - HasUnreadMessage: {channelInfo.Value.HasUnreadMessage} - HasUnreadMention: {channelInfo.Value.HasUnreadMention}");
      }

      HasUnreadMessages = _channelsInfo.Any(x => x.Value.HasUnreadMessage);
      HasUnreadMentions = _channelsInfo.Any(x => x.Value.HasUnreadMention);

      _logger.LogDebug($"HasUnreadMention: {HasUnreadMentions}");
      _logger.LogDebug($"HasUnreadMessage: {HasUnreadMessages}");

      Changed?.Invoke();
    }

    private async Task<SlackSocketClient?> ConnectSlackClientAsync()
    {
      using var connectionEvent = new ManualResetEvent(false);
      using var connectionSocketEvent = new ManualResetEvent(false);
      var newClient = _slackSocketClientFactory(_token);

      try
      {
        // ReSharper disable AccessToDisposedClosure
        newClient.Connect(_ => connectionEvent.Set(), () => connectionSocketEvent.Set());
        // ReSharper restore AccessToDisposedClosure

        var result = await Task.WhenAll(connectionEvent.WaitOneAsync(Timeout), connectionSocketEvent.WaitOneAsync(Timeout));
        if (result.All(x => x))
        {
          return newClient;
        }

        _logger.LogError("Timeout while connecting");
        return null;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Unable to connect");
        return null;
      }
      finally
      {
        connectionEvent.Set();
        connectionSocketEvent.Set();
      }
    }

    private void OnPresenceChanged(bool isActive)
    {
      IsAway = !isActive;
      Changed?.Invoke();
    }

    private ILogger CreateLogger<T>()
    {
      return _loggerFactory.CreateLogger($"{typeof(T).FullName} ({GetHashCode()})");
    }
  }
}
