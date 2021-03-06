using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.TextTasks;
using static Nuke.Common.Logger;
using static Nuke.Common.Tools.GitVersion.GitVersionTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution Solution;

    [GitVersion(Framework = "netcoreapp2.1")]
    readonly GitVersion GitVersion;

    [PackageExecutable(
        packageId: "NSIS-Tool",
        packageExecutable: "makensis.exe",
        Version = "3.0.5",
        Framework = "tools")]
    readonly Tool MakeNsis;

    AbsolutePath SourceDirectory => RootDirectory / "src";

    AbsolutePath OutputDirectory => RootDirectory / "bin" / Configuration;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    AbsolutePath ArtifactFile => ArtifactsDirectory / $"{Solution.Name}-{GitVersion.FullSemVer}.zip";

    AbsolutePath InstallerFile => SourceDirectory / "Luxa4Slack.Installer" / "Luxa4Slack.Installer.nsi";

    AbsolutePath InstallerVersionsFile => SourceDirectory / "Luxa4Slack.Installer" / "Versions.nsh";

    AbsolutePath GlobalAssemblyInfoFile => SourceDirectory / "GlobalAssemblyInfo.cs";

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
            NuGetRestore(_ => _
                .SetTargetPath(Solution)
            );
        });

    Target PatchVersion => _ => _
        .Executes(() =>
        {
            Info("Patch " + GlobalAssemblyInfoFile);
            GitVersion(_ => _
                .SetFramework("netcoreapp2.1")
                .SetArgumentConfigurator(_ => _
                    .Add("/updateassemblyinfo {value}", GlobalAssemblyInfoFile)
                )
            );

            Info("Patch " + InstallerVersionsFile);
            WriteAllLines(InstallerVersionsFile,
                new[]
                {
                    $"!define VERSIONMAJOR {GitVersion.Major}",
                    $"!define VERSIONMINOR {GitVersion.Minor}",
                    $"!define VERSIONPATCH {GitVersion.Patch}"
                });
        });

    Target Compile => _ => _
        .DependsOn(Restore, PatchVersion)
        .Executes(() =>
        {
            MSBuild(_ => _
                .SetTargetPath(Solution)
                .SetTargets("Rebuild")
                .SetConfiguration(Configuration)
                .SetMaxCpuCount(Environment.ProcessorCount)
                .SetNodeReuse(IsLocalBuild));
        });

    Target Pack => _ => _
        .DependsOn(Clean, Compile)
        .Executes(() =>
        {
            var extensions = new[] { ".exe", ".exe.config", ".dll" };

            CompressZip(
                OutputDirectory,
                ArtifactFile,
                info => extensions.Any(extension => info.Name.EndsWith(extension)));
        });

    Target BuildInstaller => _ => _
        .DependsOn(Clean, Compile)
        .Executes(() =>
        {
            MakeNsis(
                arguments: $"/V4 /DCONFIGURATION={Configuration} {InstallerFile}",
                workingDirectory: InstallerFile.Parent,
                logOutput: true);
        });
}
