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
    private readonly IOptionsMonitor<T> _options;
    private readonly string _section;
    private readonly string _filePath;
    private readonly ILogger _logger;

    public WritableOptions(
      IOptionsMonitor<T> options,
      string section,
      string filePath,
      ILogger logger)
    {
      _options = options;
      _section = section;
      _filePath = filePath;
      _logger = logger;
    }

    public T Value => _options.CurrentValue;
    public T Get(string name) => _options.Get(name);

    public void Update(Action<T> applyChanges)
    {
      var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(_filePath));
      if (jObject == null)
      {
        _logger.LogError($"Unable to deserialize '{_filePath}'");
      }
      else
      {
        var sectionObject = (jObject.TryGetValue(_section, out var jsonSection)
          ? JsonConvert.DeserializeObject<T>(jsonSection.ToString())
          : Value) ?? new T();

        applyChanges(sectionObject);

        jObject[_section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
        _logger.LogDebug($"Write options to '{_filePath}'");
        File.WriteAllText(_filePath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
      }
    }
  }
}
