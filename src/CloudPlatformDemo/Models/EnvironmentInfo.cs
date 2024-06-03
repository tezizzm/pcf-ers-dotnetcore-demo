using CloudPlatformDemo.Workaround;
using Steeltoe.Common;

namespace CloudPlatformDemo.Models;

public class EnvironmentInfo
{
    private readonly IConfiguration _configuration;

    public EnvironmentInfo(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsContainerCertIdentityConfigured => IsCloudFoundry && _configuration.GetValue<string>("certificate") != null && _configuration["privateKey"] != null;
    //todo: figure out logic for if service is bound
    public bool IsSsoBound => _configuration.GetServiceBindings().IsServiceBound("SQLServer");
    public bool IsMySqlBound => _configuration.GetServiceBindings().IsServiceBound("p.mysql", "mysql");
    public bool IsRabbitMqBound => _configuration.GetServiceBindings().IsServiceBound("p.rabbitmq", "rabbitmq");
    public bool IsEurekaBound => _configuration.GetServiceBindings().IsServiceBound("eureka","p.service-registry");
    public bool IsSqlServerBound => false;
    public bool IsConfigServerBound => _configuration
        .GetSection("spring:cloud")
        .GetChildren()
        .Any(x => x.Key.Equals("config", StringComparison.InvariantCultureIgnoreCase)) ||
        _configuration.GetServiceBindings().IsServiceBound("p.config-server");
    public bool IsCloudFoundry => Platform.IsCloudFoundry;
    public bool IsAzureSpringApps => Platform2.IsAzureSpringApps;
}