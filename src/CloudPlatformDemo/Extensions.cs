using Steeltoe.Common;

namespace CloudPlatformDemo;

public static class Extensions
{
    public static ServiceBindings GetServiceBindings(this IConfiguration configuration)
    {
        return Platform.IsCloudFoundry ? configuration.GetServiceBindingsCloudFoundry() : configuration.GetServiceBindingsKubernetes();
        
    }

    private static ServiceBindings GetServiceBindingsKubernetes(this IConfiguration configuration)
    {
        var configSection = "vcap:services";
        var options = new ServiceBindings();
        var allServicesConfig = configuration.GetSection(configSection);
        var serviceClassProperties = typeof(Service).GetProperties().Select(x => x.Name.ToLower()).ToHashSet();
        foreach (var serviceSection in allServicesConfig.GetChildren())
        {
            var service = new Service
            {
                Name = serviceSection.Key,
                AdditionalProperties = new()
            };
            serviceSection.Bind(service);
            
            foreach (var property in serviceSection.GetChildren().Where(x => !serviceClassProperties.Contains(x.Key.ToLower())))
            {
                service.AdditionalProperties.Add(property.Key, property.Value);
            }
            options.Services.Add(service);
        }

        return options;
    }
    private static ServiceBindings GetServiceBindingsCloudFoundry(this IConfiguration configuration)
    {
        var configSection = "vcap:services";
        var options = new ServiceBindings();
        var allServicesConfig = configuration.GetSection(configSection);
        var serviceClassProperties = typeof(Service).GetProperties().Select(x => x.Name.ToLower()).ToHashSet();
        foreach (var serviceTypeSection in allServicesConfig.GetChildren())
        {
            foreach (var serviceSection in serviceTypeSection.GetChildren())
            {
                var service = new Service
                {
                    Type = serviceTypeSection.Key,
                    AdditionalProperties = new()
                };
                serviceSection.Bind(service);

                foreach (var property in serviceSection.GetChildren()
                             .Where(x => !serviceClassProperties.Contains(x.Key.ToLower())))
                {
                    if (property.Key.ToLower() == "credentials")
                    {
                        foreach (var credentialAttribute in property.GetChildren())
                        {
                            service.AdditionalProperties.Add(credentialAttribute.Key, credentialAttribute.Value);
                        }

                        continue;
                    }
                    service.AdditionalProperties.Add(property.Key, property.Value);
                }

                options.Services.Add(service);
            }
        }

        return options;
    }
}