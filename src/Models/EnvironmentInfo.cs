using Articulate.Workaround;
using Steeltoe.Common;
using Steeltoe.Connector.Services;

namespace Articulate.Models;

public class EnvironmentInfo
{
    private readonly IConfiguration _configuration;

    public EnvironmentInfo(IConfiguration configuration)
    {
        _configuration = configuration;
        IsConfigServerBound = _configuration.GetSection("spring:cloud").GetChildren()
            .Any(x => x.Key.Equals("config", StringComparison.InvariantCultureIgnoreCase));
        IsEurekaBound = _configuration.IsServiceBound<EurekaServiceInfo>() || _configuration.GetValue<string>("Eureka:Client:ServiceUrl") != null;
        IsMySqlBound = _configuration.IsServiceBound<MySqlServiceInfo>();
        IsSqlServerBound = _configuration.IsServiceBound<SqlServerServiceInfo>();
        IsSsoBound = _configuration.IsServiceBound<SsoServiceInfo>();
        IsContainerCertIdentityConfigured = _configuration.GetValue<string>("certificate") != null;
        //IsAzureSpringApps = Directory.Exists("/etc/azure-spring-cloud") || Environment.GetEnvironmentVariables().Keys.Cast<string>().Any(x => x.StartsWith("ASCSVCRT_"));
        
    }

    public bool IsContainerCertIdentityConfigured { get; }
    public bool IsSsoBound { get; }
    public bool IsMySqlBound { get;  }
    public bool IsEurekaBound { get;  }
    public bool IsSqlServerBound { get; }
    public bool IsConfigServerBound { get;  }
    public bool IsCloudFoundry => Platform.IsCloudFoundry;
    //public bool IsAzureSpringApps { get; set; }
}