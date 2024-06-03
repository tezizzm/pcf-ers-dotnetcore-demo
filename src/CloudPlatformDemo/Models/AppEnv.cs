using CloudPlatformDemo.Utils;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Configuration;
// using Steeltoe.CloudFoundry.Connector.App;
using Service = CloudPlatformDemo.Utils.Service;

namespace CloudPlatformDemo.Models;

public class AppEnv
{
    public AppEnv(IHttpContextAccessor context, 
        IApplicationInstanceInfo appInfo, 
        IOptionsSnapshot<ServiceBindings> services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        var connectionContext = context.HttpContext.Features.Get<IHttpConnectionFeature>();
        ContainerAddress = $"{connectionContext.LocalIpAddress}:{connectionContext.LocalPort}";
        AppName = appInfo.ApplicationName ?? configuration.GetValue<string>("Spring:Application:Name");
        DeploymentName = configuration.GetValue<string>("AZURE_SPRING_APPS:DEPLOYMENT:NAME") ?? AppName;
        InstanceName =  !string.IsNullOrEmpty(appInfo.InstanceId) ? appInfo.InstanceId : System.Environment.GetEnvironmentVariable("CF_INSTANCE_GUID") ?? System.Environment.GetEnvironmentVariable("HOSTNAME");
        Services = services.Value.Services.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.ToList());
        ClrVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        HostAddress = System.Environment.GetEnvironmentVariable("CF_INSTANCE_ADDR") ?? System.Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") ?? "localhost";
        Environment = environment.EnvironmentName;
        Profiles = configuration.GetValue<string>("spring:profiles:active");
    }

    public string DeploymentName { get; set; }

    object MapCredentials(Credential credentials)
    {
        if (credentials.Value != null)
            return credentials.Value;
        return credentials.ToDictionary(x => x.Key, x => MapCredentials(x.Value));
    }

    public string Environment { get; }
    public string Profiles { get; }
    public string HostAddress { get; }
    public string ContainerAddress { get; }
    public string AppName { get; set; }
    public string InstanceName { get; set; }
    public Dictionary<string, List<Service>> Services { get; }
    public string ClrVersion { get; }
        
}

public class ServiceBinding
{
    /// <summary>
    /// Gets or sets the name of the service instance
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a label describing the type of service
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the plan level at which the service is provisoned
    /// </summary>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets a list of tags describing the service
    /// </summary>
    public string Plan { get; set; }
        
    public Dictionary<string,object> Credentials { get; set; }
}