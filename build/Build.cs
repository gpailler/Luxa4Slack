using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.TextTasks;
using static Nuke.Common.IO.CompressionTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitVersion(Framework = "net5.0", NoFetch = true)] readonly GitVersion GitVersion;

    [PackageExecutable(
      packageId: "NSIS-Tool",
      packageExecutable: "makensis.exe",
      Version = "3.0.6.1",
      Framework = "tools")]
    readonly Tool MakeNsis;

    AbsolutePath SourceDirectory => RootDirectory / "src";

    AbsolutePath OutputDirectory => RootDirectory / "bin" / Configuration;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    AbsolutePath ArtifactFile => ArtifactsDirectory / $"{Solution.Name}-{GitVersion.FullSemVer}.zip";

    AbsolutePath InstallerFile => SourceDirectory / "Luxa4Slack.Installer" / "Luxa4Slack.Installer.nsi";

    AbsolutePath InstallerVersionsFile => SourceDirectory / "Luxa4Slack.Installer" / "Versions.nsh";

    AbsolutePath PublishedProjectDirectory => SourceDirectory / "Luxa4Slack.Tray";

    const string PublishPlatform = "win-x64";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);

            EnsureCleanDirectory(OutputDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
      .DependsOn(Restore)
      .Executes(() =>
      {
        DotNetBuild(s => s
          .SetProjectFile(Solution)
          .SetConfiguration(Configuration)
          .SetAssemblyVersion(GitVersion.AssemblySemVer)
          .SetFileVersion(GitVersion.AssemblySemFileVer)
          .SetInformationalVersion(GitVersion.InformationalVersion)
          .SetNoRestore(true));
      });

    Target Publish => _ => _
      .DependsOn(Clean)
      .Executes(() =>
      {
        DotNetPublish(s => s
          .SetProject(PublishedProjectDirectory)
          .SetConfiguration(Configuration)
          .SetAssemblyVersion(GitVersion.AssemblySemVer)
          .SetFileVersion(GitVersion.AssemblySemFileVer)
          .SetInformationalVersion(GitVersion.InformationalVersion)
          .SetRuntime(PublishPlatform)
          .SetOutput(OutputDirectory)
          .SetSelfContained(true)
          .SetPublishSingleFile(false));
      });

    Target Pack => _ => _
      .DependsOn(Clean, Publish)
      .Executes(() =>
      {
        var extensions = new[] { ".exe", ".exe.config", ".dll", "*.pdb", "*.json", "*.config" };

        CompressZip(
          OutputDirectory,
          ArtifactFile,
          info => extensions.Any(extension => info.Name.EndsWith(extension)));
      });

    Target BuildInstaller => _ => _
      .DependsOn(Clean, Publish)
      .Executes(() =>
      {
        WriteAllLines(InstallerVersionsFile,
          new[]
          {
            $"!define VERSIONMAJOR {GitVersion.Major}",
            $"!define VERSIONMINOR {GitVersion.Minor}",
            $"!define VERSIONPATCH {GitVersion.Patch}"
          });

        MakeNsis(
          arguments: $"/V4 /DCONFIGURATION={Configuration} {InstallerFile}",
          workingDirectory: InstallerFile.Parent,
          logOutput: true);
      });
}
