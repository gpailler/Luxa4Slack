namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using CG.Luxa4Slack.Abstractions;
  using CG.Luxa4Slack.Extensions;
  using CG.Luxa4Slack.Notifications.Converters;
  using Microsoft.Extensions.Logging;
  using Polly;
  using Polly.Retry;
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal abstract class MessageHandlerBase : IMessageHandler
  {
    protected const int HistoryItemsToFetch = 100;
    protected const int MaxDegreeOfParallelism = 2;

    protected readonly SlackSocketClient Client;
    protected readonly HandlerContext Context;
    protected readonly ILogger Logger;

    private readonly AsyncRetryPolicy _retryPolicy;

    protected MessageHandlerBase(SlackSocketClient client, HandlerContext context, ILogger logger)
    {
      Client = client;
      Context = context;
      Logger = logger;

      _retryPolicy = Policy
        .Handle<RateLimitedException>()
        .WaitAndRetryAsync(
          10,
          retryAttempt => TimeSpan.FromSeconds(5 * retryAttempt),
          (_, timeSpan, retry, _) => Logger.LogWarning($"Rate limited exception. Attempt {retry}, Wait {timeSpan}"));

      Client.BindCallback<NewMessage>(OnMessageReceived);
    }

    public abstract Task InitializeAsync();

    public virtual void Dispose()
    {
      Client.UnbindCallback<NewMessage>(OnMessageReceived);
    }

    protected async Task<TResult> RunSlackClientMethodAsync<TMessage, TResult>(
      Action<Action<TMessage>> slackClientMethod, Func<TMessage, TResult> messageExtractor)
      where TMessage : Response
    {
      return await _retryPolicy
        .ExecuteAsync(() => RunSlackClientMethodWithUnwrappedExceptionAsync(slackClientMethod, messageExtractor));
    }

    protected static string? GetRawMessage(SlackSocketMessage message)
    {
      // ReSharper disable once SuspiciousTypeConversion.Global
      if (message is not IRawMessage rawMessage)
      {
        throw new InvalidCastException($"'{message.GetType().FullName}' is not a proxy class and cannot be casted to IRawMessage");
      }
      else
      {
        return rawMessage.Data;
      }
    }

    protected bool HasMention(string? text)
    {
      return text != null && Context.HighlightWords.Any(x => text.IndexOf(x, StringComparison.InvariantCultureIgnoreCase) != -1);
    }

    protected bool IsRegularMessage(Message message)
    {
      return IsRegularMessage(message.user, message.subtype);
    }

    protected static bool FilterMessageByDate(Message message, DateTime minDate)
    {
      // Drop messages from threads
      return message.ts > minDate && message.thread_ts == null;
    }

    protected abstract bool ShouldMonitor(string id);

    private bool IsRegularMessage(string user, string? subtype)
    {
      return user != Client.MySelf.id && (subtype == null || subtype == "file_share" || subtype == "bot_message");
    }

    private void OnMessageReceived(NewMessage message)
    {
      if (message.type == "message")
      {
        if (!Context.MutedChannels.Contains(message.channel)
            && IsRegularMessage(message.user, message.subtype)
            && ShouldMonitor(message.channel)
            && (message.thread_ts == DateTime.MinValue || HasMention(GetRawMessage(message))))
        {
          Logger.LogDebug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {Context.GetNameFromId(message.channel)} - Raw: {GetRawMessage(message)}");

          if (Client.ChannelLookup.ContainsKey(message.channel) || Client.GroupLookup.ContainsKey(message.channel))
          {
            Context.ChannelsInfo[message.channel].Update(true, Convert.ToInt32(HasMention(GetRawMessage(message))) > 0);
          }
          else
          {
            Context.ChannelsInfo[message.channel].Update(true, true);
          }
        }
      }
    }

    private static async Task<TResult> RunSlackClientMethodWithUnwrappedExceptionAsync<TMessage, TResult>(Action<Action<TMessage>> slackClientMethod, Func<TMessage, TResult> messageExtractor)
      where TMessage : Response
    {
      TResult? result = default;
      Exception? exception = null;

      using var waiter = new ManualResetEvent(false);
      slackClientMethod(
        response =>
        {
          try
          {
            if (!response.ok)
            {
              if (response.error == "ratelimited")
              {
                exception = new RateLimitedException();
              }
              else
              {
                exception = new Exception(response.error);
              }
            }

            result = messageExtractor(response);
          }
          finally
          {
            // ReSharper disable once AccessToDisposedClosure
            waiter.Set();
          }
        });

      await waiter.WaitOneAsync(SlackNotificationAgent.Timeout);

      if (exception != null)
      {
        throw exception;
      }

      return result!;
    }
  }
}
