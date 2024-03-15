using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration;

public static class ConfigurationHelper
{
    public static ConfigurationManager PostProcessKubernetesBindings(ConfigurationManager config)
    {
        var transformedConfig = config.GetSection("k8s:bindings")
            .AsEnumerable()
            .ToDictionary(x => $"services:{x.Key}", x => x.Value);
        config.AddInMemoryCollection(transformedConfig);
        return config;
    }
}