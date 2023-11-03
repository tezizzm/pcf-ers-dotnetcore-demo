using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Serilog;

public partial class Build
{
    [Parameter("Azure Spring Apps Service Name")]
    string AsaServiceName;
    [Parameter("Azure Resource Group that owns Azure Spring Apps instance")]
    string AsaResourceGroup;
    [Parameter("Azure Spring Apps Deployment name")]
    string Deployment;

    string GitRepoUri = "https://github.com/macsux/pcf-ers-dotnetcore-demo";

    string ConfigServicePattern = "application,cpd,cpd-green,cpd-blue";

    Target AsaDelete => _ => _
        .Executes(() =>
        {
            TryAzureSpringApps($"spring application-configuration-service unbind --app {AppName}");
            TryAzureSpringApps($"spring application-configuration-service git repo remove --name {AppName}");
            TryAzureSpringApps($"spring service-registry unbind --app {AppName}");
            TryAzureSpringApps($"spring app delete --name {AppName}");
        });
    Target AsaDeploy => _ => _
        .DependsOn(Pack, AsaEnsureReadyForDeployment)
        .Requires(() => AsaServiceName, () => AsaResourceGroup, () => AppName)
        .Executes(() =>
        {
            var deploymentNames = Deployment == null ? new[] { "green", "blue" } : new[] { Deployment };
            Log.Logger.Information("Deployment name not set: deploying to both 'green' and 'blue'");
            foreach (var deployment in deploymentNames)
            {
                AzureSpringApps(
                    $"spring app deploy --service {AsaServiceName} -g {AsaResourceGroup} -n {AppName} --artifact-path {ArtifactsDirectory / PackageZipName} --config-file-pattern {ConfigServicePattern} --deployment {deployment}");
            }
        });


    Target AsaEnsureReadyForDeployment => _ => _
        .DependsOn(AsaEnsureConfigServiceConfigured, AsaEnsureEurekaExists, AsaEnsureAzureAppExists, AsaEnsureDeploymentsExist, AsaEnsureAppIsBound)
        .Executes(() =>
        {
        });

    Target AsaEnsureAzureAppExists => _ => _
        .Unlisted()
        .Requires(() => AsaServiceName, () => AsaResourceGroup, () => AppName)
        .Executes(() =>
        {
            var apps = GetAppNames();
            if (apps.Contains(AppName))
                return;
            AzureSpringApps($"spring app create --service {AsaServiceName} -g {AsaResourceGroup} -n {AppName} --deployment-name green");
        });
    
    Target AsaEnsureDeploymentsExist => _ => _
        .Unlisted()
        .Requires(() => AsaServiceName, () => AsaResourceGroup, () => AppName)
        .Executes(() =>
        {
            var existingDeployments = GetDeploymentNames();
            var deploymentsToCreate = new HashSet<string> { "blue", "green" };
            deploymentsToCreate.ExceptWith(existingDeployments);
            foreach (var deployment in deploymentsToCreate)
            {
                AzureSpringApps($"spring app deployment create --config-file-patterns {ConfigServicePattern} --service {AsaServiceName} -g {AsaResourceGroup} --app {AppName} -n {deployment}");
            }
        });

    Target AsaEnsureEurekaExists => _ => _
        .Unlisted()
        .Executes(() =>
        {
            if (!TryAzureSpringApps($"spring service-registry show --service {AsaServiceName} -g {AsaResourceGroup}"))
            {
                AzureSpringApps($"spring service-registry create --service {AsaServiceName} -g {AsaResourceGroup}");
            }
        });

    Target AsaEnsureAppIsBound => _ => _
        .After(AsaEnsureConfigServiceConfigured, AsaEnsureEurekaExists, AsaEnsureAzureAppExists)
        .Executes(() =>
        {
            AzureSpringApps($"spring service-registry bind --app {AppName} --service {AsaServiceName} -g {AsaResourceGroup}");
            AzureSpringApps($"spring application-configuration-service bind --app {AppName} --service {AsaServiceName} -g {AsaResourceGroup}");
        });

    Target AsaEnsureConfigServiceConfigured => _ => _
        .Unlisted()
        .Before(AsaEnsureAzureAppExists)
        .Requires(() => AsaServiceName, () => AsaResourceGroup, () => AppName)
        .Executes(() =>
        {
            if (!TryAzureSpringApps($"spring application-configuration-service  show --service {AsaServiceName} -g {AsaResourceGroup}"))
            {
                AzureSpringApps($"spring application-configuration-service  create --service {AsaServiceName} -g {AsaResourceGroup}");
            }
            var repos = GetConfigServiceRepos();
            if (repos.Any(x => x.StartsWith(GitRepoUri)))
            {
                AddConfigServiceGitRepo();
            }
        });
    
    void AddConfigServiceGitRepo() => AzureSpringApps($"spring application-configuration-service git repo add --name {AppName} --patterns {ConfigServicePattern} --uri {GitRepoUri} --label master --search-paths config --service {AsaServiceName} -g {AsaResourceGroup}");

    HashSet<string> GetAppNames()
    {
        var json = AzureSpringApps<JArray>($"spring app list --service {AsaServiceName} -g {AsaResourceGroup}");
        return json.SelectTokens(".[*].name").Select(x => x.Value<string>()).ToHashSet();
    }
    
    HashSet<string> GetDeploymentNames()
    {
        var json = AzureSpringApps<JArray>($"spring app deployment list --service {AsaServiceName} -g {AsaResourceGroup} --app {AppName}");
        return json.SelectTokens(".[*].name").Select(x => x.Value<string>()).ToHashSet();
    }
    
    HashSet<string> GetConfigServiceRepos()
    {
        
        if (TryAzureSpringApps($"spring application-configuration-service git repo list --service {AsaServiceName} -g {AsaResourceGroup}", out var result))
        {
            return  JArray.Parse(result).SelectTokens(".[*].name").Select(x => x.Value<string>()).ToHashSet();
        }

        if (result.Contains("'NoneType' object has no attribute 'git_property'")) // no repos have been created
        {
            return new HashSet<string>();
        }

        throw new Exception(result);
    }


    static JToken AzureSpringApps(string args) => AzureSpringApps<JToken>(args);
    static T AzureSpringApps<T>(string args)
    {
        var az = ToolPathResolver.GetPathExecutable("az");
        var process = ProcessTasks.StartProcess(az, args);
        process.AssertZeroExitCode();
        return process.Output.StdToJson<T>();
    }

    static bool TryAzureSpringApps(string args)
    {
        return TryAzureSpringApps(args, out _);
    }
    static bool TryAzureSpringApps(string args, out string result)
    {
        var isSuccess = TryAzureSpringApps(args, out var std, out var err);
        result = string.Join("\n", std, err);
        return isSuccess;
    }

    static bool TryAzureSpringApps(string args, out string std, out string err)
    {
        var az = ToolPathResolver.GetPathExecutable("az");
        var process = ProcessTasks.StartProcess(az, args);
        process.WaitForExit();
        std = process.Output.Where(x => x.Type == OutputType.Std)
            .Select(x => x.Text)
            .JoinNewLine();
        err = process.Output.Where(x => x.Type == OutputType.Err)
            .Select(x => x.Text)
            .JoinNewLine();
        
        return process.ExitCode == 0;
    }
}