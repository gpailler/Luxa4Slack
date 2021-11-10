namespace CG.Luxa4Slack.Notifications.Converters
{
  using System;
  using Newtonsoft.Json;
  using SlackAPI;

  internal class StringToTextConverter : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
      var instance = new Text();
      if (reader.TokenType == JsonToken.String)
      {
        // Handle blocks containing only a string instead of a Text object (Giphy messages for example)
        instance.text = reader.Value as string;
      }
      else
      {
        serializer.Populate(reader, instance);
      }

      return instance;
    }

    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(Text);
    }
  }
}
