namespace CG.Luxa4Slack.Tray
{
  using System.Diagnostics;
  using System.IO;
  using System.Reflection;

  public class ApplicationInfo
  {
    public ApplicationInfo()
    {
      var fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
      Name = fileVersionInfo.ProductName;
      Version = fileVersionInfo.FileVersion;
      ApplicationPath = Path.GetDirectoryName(fileVersionInfo.FileName)!;
    }

    public string ApplicationPath { get; }

    public string DisplayName => $"{Name} ({Version})";

    public string? Name { get; }

    public string? Version { get; }

    public string Format(string text)
    {
      return $"{DisplayName} - {text}";
    }
  }
}
