namespace CG.Luxa4Slack.Notifications.Converters
{
  using System;

  using Castle.DynamicProxy;

  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  using SlackAPI;
  using SlackAPI.WebSocketMessages;

  internal class JsonRawConverter : JsonConverter
  {
    private readonly ProxyGenerator _proxyGenerator = new();

    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(ImMarked) ||
             objectType == typeof(ChannelMarked) ||
             objectType == typeof(GroupMarked) ||
             objectType == typeof(Message) ||
             objectType == typeof(NewMessage);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
      if (reader.TokenType != JsonToken.Null)
      {
        var jsonObject = JObject.Load(reader);
        var rawData = jsonObject.ToString(Formatting.None);

        if (string.IsNullOrEmpty(reader.Path) == false)
        {
          var token = jsonObject.SelectToken(reader.Path);
          if (token != null)
          {
            rawData = token.ToString();
          }
        }

        var instance = _proxyGenerator.CreateClassProxy(objectType, new[] { typeof(IRawMessage) }, new RawMessageInterceptor(rawData));
        serializer.Populate(jsonObject.CreateReader(), instance);

        return instance;
      }

      return null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }
  }
}
