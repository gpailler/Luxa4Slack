namespace CG.Luxa4Slack.Notifications.MessageHandlers
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using CG.Luxa4Slack.Notifications.Converters;
  using Microsoft.Extensions.Logging;
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal abstract class MessageHandlerBase : IDisposable
  {
    protected const int HistoryItemsToFetch = 50;

    private const int MaxDegreeOfParallelism = 8;

    protected readonly SlackSocketClient Client;
    protected readonly HandlerContext Context;
    protected readonly ILogger Logger;

    protected MessageHandlerBase(SlackSocketClient client, HandlerContext context, ILogger logger)
    {
      Client = client;
      Context = context;
      Logger = logger;

      Client.BindCallback<NewMessage>(OnMessageReceived);
    }

    public virtual void Dispose()
    {
      Client.UnbindCallback<NewMessage>(OnMessageReceived);
    }

    protected string? GetRawMessage(SlackSocketMessage message)
    {
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

    protected bool IsRegularMessage(string user, string? subtype)
    {
      return user != Client.MySelf.id && (subtype == null || subtype == "file_share" || subtype == "bot_message");
    }

    protected bool IsRegularMessage(Message message)
    {
      return IsRegularMessage(message.user, message.subtype);
    }

    protected bool FilterMessageByDate(Message message, DateTime minDate)
    {
      // Drop messages from threads
      return message.ts > minDate && message.thread_ts == null;
    }

    protected abstract bool ShouldMonitor(string id);

    protected void RunParallel<T>(IEnumerable<T> source, Action<T> callback)
    {
      Parallel.ForEach(
        source,
        new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
        callback
      );
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
  }
}
