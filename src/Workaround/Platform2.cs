namespace Articulate.Workaround;

//todo: this belongs in Steeltoe.Common.Platform
public static class Platform2
{
    public static bool IsAzureSpringApps => Directory.Exists("/etc/azure-spring-cloud") || Environment.GetEnvironmentVariables().Keys.Cast<string>().Any(x => x.StartsWith("ASCSVCRT_"));
    
}