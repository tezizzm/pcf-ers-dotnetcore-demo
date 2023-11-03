using System.Collections;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.CloudFoundry;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using Octokit;
using Serilog;
using Tools.CloudFoundry;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Docker.DockerTasks;
using static Nuke.Common.Tools.CloudFoundry.CloudFoundryTasks;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem


[UnsetVisualStudioEnvironmentVariables]
//[AzureDevopsConfigurationGenerator(
//    VcsTriggeredTargets = new[]{"Pack"}
//)]
partial class Build : NukeBuild
{
    static Build()
    {
        Environment.SetEnvironmentVariable("NUKE_TELEMETRY_OPTOUT","true");
    }
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>();

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    string Framework => "net6.0";
    [Parameter("GitHub personal access token with access to the repo")]
    string GitHubToken;

    [Solution] readonly Solution Solution;
    [GitRepository] GitRepository GitRepository;
    [NerdbankGitVersioning] readonly NerdbankGitVersioning GitVersion;
    [Parameter("App Name for deployments")]
    string AppName = "cpdemo";



    string Runtime = "linux-x64";
    string AssemblyName = "CloudPlatformDemo";
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => RootDirectory / "src" / "CloudPlatformDemo" / "bin" / Configuration / Framework / Runtime  / "publish";
    string PackageZipName => $"CloudPlatformDemo-{GitVersion.SemVer2}.zip";

    AppDeployment[] Apps;
    AppDeployment Green, Blue, Backend;

    
    Target Clean => _ => _
        .Before(Restore)
        .Description("Clean out bin/obj folders")
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Description("Restore nuget packages")
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Description("Compiles code for local execution")
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblyVersion)
                .SetFileVersion(GitVersion.AssemblyFileVersion)
                .SetInformationalVersion(GitVersion.AssemblyInformationalVersion)
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .Description("Publishes the project to a folder which is ready to be deployed to target machines")
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetRuntime("linux-x64")
                .DisableSelfContained()
                .SetAssemblyVersion(GitVersion.AssemblyVersion)
                .SetFileVersion(GitVersion.AssemblyFileVersion)
                .SetInformationalVersion(GitVersion.AssemblyInformationalVersion));
        });

    Target Pack => _ => _
        .DependsOn(Publish)
        .Description("Publishes the project and creates a zip package in artifacts folder")
        .Produces(ArtifactsDirectory)
        .Executes(() =>
        {
            Directory.CreateDirectory(ArtifactsDirectory);
            DeleteFile(ArtifactsDirectory / PackageZipName);
            ZipFile.CreateFromDirectory(PublishDirectory, ArtifactsDirectory / PackageZipName);
            Log.Information(ArtifactsDirectory / PackageZipName);
        });



    Target Release => _ => _
        .Description("Creates a GitHub release (or amends existing) and uploads the artifact")
        .DependsOn(Publish)
        .Requires(() => GitHubToken)
        .Executes(async () =>
        {
            if (!GitRepository.IsGitHubRepository())
                Assert.Fail("Only supported when git repo remote is github");
            if(!IsGitPushedToRemote)
                Assert.Fail("Your local git repo has not been pushed to remote. Can't create release until source is upload");
            var client = new GitHubClient(new ProductHeaderValue("nuke-build"))
            {
                Credentials = new Credentials(GitHubToken, AuthenticationType.Bearer)
            };
            var gitIdParts = GitRepository.Identifier.Split("/");
            var owner = gitIdParts[0];
            var repoName = gitIdParts[1];
            
            var releaseName = $"v{GitVersion.SemVer2}";
            Release release;
            try
            {
                release = await client.Repository.Release.Get(owner, repoName, releaseName);
            }
            catch (NotFoundException)
            {
                var newRelease = new NewRelease(releaseName)
                {
                    Name = releaseName, 
                    Draft = false, 
                    Prerelease = false
                };
                release = await client.Repository.Release.Create(owner, repoName, newRelease);
            }

            var existingAsset = release.Assets.FirstOrDefault(x => x.Name == PackageZipName);
            if (existingAsset != null)
            {
                await client.Repository.Release.DeleteAsset(owner, repoName, existingAsset.Id);
            }
            
            var zipPackageLocation = ArtifactsDirectory / PackageZipName;
            var releaseAssetUpload = new ReleaseAssetUpload(PackageZipName, "application/zip", File.OpenRead(zipPackageLocation), null);
            var releaseAsset = await client.Repository.Release.UploadAsset(release, releaseAssetUpload);
            
            Log.Information(releaseAsset.BrowserDownloadUrl);
        });


    bool IsGitPushedToRemote => GitTasks
        .Git("status")
        .Select(x => x.Text)
        .Count(x => x.Contains("nothing to commit, working tree clean") || x.StartsWith("Your branch is up to date with")) == 2;

    Target RunSpringBootAdmin => _ => _
        .Executes(async () =>
        {
            var containerName = "spring-boot-admin";
            IReadOnlyCollection<Output> output = new Output[0];
            await Task.WhenAny(Task.Run(() =>
                output = DockerRun(c => c
                    .SetImage("steeltoeoss/spring-boot-admin")
                    .EnableRm()
                    .SetName(containerName)
                    .SetAttach("STDOUT", "STDERR")
                    .SetPublish("9090:8080"))
            ), Task.Delay(TimeSpan.FromSeconds(10)));
            
            output.EnsureOnlyStd();
            Log.Information("Press ENTER to shutdown...");
            Console.ReadLine();
            DockerKill(c => c
                .SetContainers(containerName));
        });
}