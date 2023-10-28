namespace CloudPlatformDemo.Utils;

public class KubernetesServiceConfigureOptions : ServiceConfigureOptions
{
    public KubernetesServiceConfigureOptions(IConfiguration configuration) : base(configuration, "k8s:bindings")
    {
    }
}