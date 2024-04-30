namespace CloudPlatformDemo.Utils;

/// <summary>
/// Reads VCAP_SERVICES from configuration and configures a typed <see cref="ServiceBindings"/> options class of all the service bindings for the app on Cloud Foundry 
/// </summary>
public class CloudFoundryServiceConfigureOptions : ServiceConfigureOptions
{
    public CloudFoundryServiceConfigureOptions(IConfiguration configuration) : base(configuration, "vcap:services")
    {
        
    }

    public override void Configure(ServiceBindings options)
    {
        var allServicesConfig = _configuration.GetSection(_servicesSection);
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
    }
}