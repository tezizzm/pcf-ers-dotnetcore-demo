using Nuke.Common;
using Nuke.Common.Tooling;

public partial class Build
{
    [Parameter("Azure Spring Apps Service Name")]
    string AsaServiceName;
    [Parameter("Azure Resource Group that owns Azure Spring Apps instance")]
    string AsaResourceGroup;

    Target AzureDeploy => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            AzureSpringApps(
                $"spring app deploy --service {AsaServiceName} -g {AsaResourceGroup} -n {AppName} --artifact-path {ArtifactsDirectory / PackageZipName} --config-file-pattern {AppName}");
        });


    private static IReadOnlyCollection<Output>  AzureSpringApps(string args)
    {
        var az = ToolPathResolver.GetPathExecutable("az");
        var process = ProcessTasks.StartProcess(az, args);
        process.AssertZeroExitCode();
        return process.Output;
    }
}