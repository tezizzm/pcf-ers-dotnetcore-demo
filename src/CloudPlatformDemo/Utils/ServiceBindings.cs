
namespace CloudPlatformDemo.Utils;

public class ServiceBindings
{
    public List<Service> Services { get; set; } = new();

    public bool IsServiceBound(params string[] types)
    {
        var services = Services.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.ToList());
        foreach (var serviceType in types)
        {
            if (services.ContainsKey(serviceType))
                return true;
        }

        return false;
    }
}