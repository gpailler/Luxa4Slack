namespace CG.Luxa4Slack.Tray.Options
{
  using System;
  using System.IO;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Options;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;
  using ILogger = Microsoft.Extensions.Logging.ILogger;

  public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
  {
    private readonly IOptionsMonitor<T> options;
    private readonly string section;
    private readonly string filePath;
    private readonly ILogger logger;

    public WritableOptions(
      IOptionsMonitor<T> options,
      string section,
      string filePath,
      ILogger logger)
    {
      this.options = options;
      this.section = section;
      this.filePath = filePath;
      this.logger = logger;
    }

    public T Value => this.options.CurrentValue;
    public T Get(string name) => this.options.Get(name);

    public void Update(Action<T> applyChanges)
    {
      var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(this.filePath));
      if (jObject == null)
      {
        this.logger.LogError($"Unable to deserialize '{this.filePath}'");
      }
      else
      {
        var sectionObject = (jObject.TryGetValue(this.section, out JToken? jsonSection)
          ? JsonConvert.DeserializeObject<T>(jsonSection.ToString())
          : this.Value) ?? new T();

        applyChanges(sectionObject);

        jObject[this.section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
        this.logger.LogDebug($"Write options to '{this.filePath}'");
        File.WriteAllText(this.filePath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
      }
    }
  }
}
