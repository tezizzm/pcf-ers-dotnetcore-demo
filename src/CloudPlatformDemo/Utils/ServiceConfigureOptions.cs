using Microsoft.Extensions.Options;

namespace CloudPlatformDemo.Utils;

public class ServiceConfigureOptions : IConfigureOptions<ServiceBindings>
{
    protected readonly IConfiguration _configuration;
    protected readonly string _servicesSection;

    public ServiceConfigureOptions(IConfiguration configuration, string servicesSection)
    {
        _configuration = configuration;
        _servicesSection = servicesSection;
    }

    public virtual void Configure(ServiceBindings options)
    {
        var allServicesConfig = _configuration.GetSection(_servicesSection);
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
        

    }
    
}