namespace CG.Luxa4Slack.MessageHandlers
{
  using System;
  using System.Linq;
  using NLog;
  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal abstract class MessageHandlerBase : IDisposable
  {
    protected const int HistoryItemsToFetch = 50;

    protected readonly SlackSocketClient Client;

    protected readonly HandlerContext Context;

    protected readonly ILogger Logger;

    protected MessageHandlerBase(SlackSocketClient client, HandlerContext context, ILogger logger)
    {
      this.Client = client;
      this.Context = context;
      this.Logger = logger;

      this.Client.BindCallback<NewMessage>(this.OnMessageReceived);
    }

    public virtual void Dispose()
    {
      this.Client.UnbindCallback<NewMessage>(this.OnMessageReceived);
    }

    protected string GetRawMessage(SlackSocketMessage message)
    {
      if (!(message is IRawMessage rawMessage))
      {
        throw new InvalidCastException($"'{message.GetType().FullName}' is not a proxy class and cannot be casted to IRawMessage");
      }
      else
      {
        return rawMessage.Data;
      }
    }

    protected bool HasMention(string text)
    {
      return text != null && this.Context.HighlightWords.Any(x => text.IndexOf(x, StringComparison.InvariantCultureIgnoreCase) != -1);
    }

    protected bool IsRegularMessage(string user, string subtype)
    {
      return user != this.Client.MySelf.id && (subtype == null || subtype == "file_share" || subtype == "bot_message");
    }

    protected bool IsRegularMessage(Message message)
    {
      return this.IsRegularMessage(message.user, message.subtype);
    }

    protected bool FilterMessageByDate(Message message, DateTime minDate)
    {
      // Drop messages from threads
      return message.ts > minDate && message.thread_ts == null;
    }

    protected abstract bool ShouldMonitor(string id);

    private void OnMessageReceived(NewMessage message)
    {
      if (message.type == "message")
      {
        if (!this.Context.MutedChannels.Contains(message.channel)
            && this.IsRegularMessage(message.user, message.subtype)
            && this.ShouldMonitor(message.channel)
            && (message.thread_ts == DateTime.MinValue || this.HasMention(this.GetRawMessage(message))))
        {
          this.Logger.Debug($"Received => Type: {message.type} - SubType: {message.subtype} - Channel: {this.Context.GetNameFromId(message.channel)} - Raw: {this.GetRawMessage(message)}");

          if (this.Client.ChannelLookup.ContainsKey(message.channel) || this.Client.GroupLookup.ContainsKey(message.channel))
          {
            this.Context.ChannelsInfo[message.channel].Update(true, Convert.ToInt32(this.HasMention(this.GetRawMessage(message))) > 0);
          }
          else
          {
            this.Context.ChannelsInfo[message.channel].Update(true, true);
          }
        }
      }
    }
  }
}
