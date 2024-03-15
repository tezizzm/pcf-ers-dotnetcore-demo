using Steeltoe.Common;

namespace CloudPlatformDemo.Workaround;

//todo: this belongs in Steeltoe.Common.Platform
public static class Platform2
{
    internal static string PlatformName => Environment.GetEnvironmentVariable("PLATFORM_NAME");

    public static bool IsKubernetes => Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") != null || IsAzureSpringApps || IsTanzuApplicationPlatform;
    public static bool IsAzureSpringApps => PlatformName == nameof(CloudPlatform.AzureSpringApp) || (PlatformName == null && !Platform.IsCloudFoundry && Directory.Exists("/etc/azure-spring-cloud") || Environment.GetEnvironmentVariables().Keys.Cast<string>().Any(x => x.StartsWith("ASCSVCRT_")));
    public static bool IsTanzuApplicationPlatform => PlatformName == nameof(CloudPlatform.TanzuApplicationPlatform) || (PlatformName == null && Environment.GetEnvironmentVariable("CNB_LAYERS_DIR") != null); // not ideal, hopefully they can expose some other var for this
}

public enum CloudPlatform
{
    CloudFoundry,
    TanzuApplicationPlatform,
    AzureSpringApp,
    Generic
}