using Steeltoe.Extensions.Configuration;

namespace CloudPlatformDemo.Workaround;

public class KubernetesServicesOptions : ServicesOptions
{
    public static string ServicesConfigRoot => "k8s:bindings";

    public override string CONFIGURATION_PREFIX { get; protected set; } = ServicesConfigRoot;

    // This constructor is for use with IOptions
    public KubernetesServicesOptions()
    {
    }

    public KubernetesServicesOptions(IConfigurationRoot root)
        : base(root, ServicesConfigRoot)
    {
    }

    public KubernetesServicesOptions(IConfiguration config)
        : base(config, ServicesConfigRoot)
    {
    }
}