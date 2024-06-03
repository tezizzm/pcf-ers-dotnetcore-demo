using Steeltoe.Common;

namespace CloudPlatformDemo.Utils;

public static class Extensions
{
    public static ServiceBindings GetServiceBindings(this IConfiguration configuration)
    {
        ServiceConfigureOptions configurator = Platform.IsCloudFoundry ? new CloudFoundryServiceConfigureOptions(configuration) : new KubernetesServiceConfigureOptions(configuration);
        ServiceBindings bindings = new();
        configurator.Configure(bindings);
        return bindings;
    }
}