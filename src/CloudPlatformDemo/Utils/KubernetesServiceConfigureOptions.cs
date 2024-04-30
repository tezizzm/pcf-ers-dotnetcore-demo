namespace CloudPlatformDemo.Utils;

/// <summary>
/// Reads Kubernetes service bindings from configuration and configures a typed <see cref="ServiceBindings"/> options class of all the service bindings for the app on Kubernetes 
/// </summary>
public class KubernetesServiceConfigureOptions : ServiceConfigureOptions
{
    public KubernetesServiceConfigureOptions(IConfiguration configuration) : base(configuration, "k8s:bindings")
    {
    }
}