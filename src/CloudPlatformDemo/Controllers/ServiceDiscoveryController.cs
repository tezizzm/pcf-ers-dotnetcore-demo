using CloudPlatformDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery;

namespace CloudPlatformDemo.Controllers;

public class ServiceDiscoveryController : Controller
{
    private readonly ILogger<ServiceDiscoveryController> _log;
    private readonly AppEnv _app;
    private readonly IDiscoveryClient _discoveryClient;

    public ServiceDiscoveryController(ILogger<ServiceDiscoveryController> log, AppEnv app, IDiscoveryClient discoveryClient)
    {
        _log = log;
        _app = app;
        _discoveryClient = discoveryClient;
    }

    public IActionResult Index() => View();
        
        

    public async Task<Dictionary<string, List<string>>> GetServiceDiscoveryInstances(bool includeSelf = false, CancellationToken cancellationToken = default)
    {
        var thisAppInstance = _discoveryClient.GetLocalServiceInstance();
        var services = await _discoveryClient.GetServiceIdsAsync(cancellationToken);
        var instancesTask = services
            .Select(async serviceName => new DiscoveredService
            {
                Name = serviceName, 
                Urls = (await _discoveryClient.GetInstancesAsync(serviceName, cancellationToken))
                    .Where(service => includeSelf || service != thisAppInstance)
                    .Select(x => x.Uri.ToString())
                    .Distinct()
                    .ToList()
            })
            .ToList();
            return (await Task.WhenAll(instancesTask)).ToDictionary(x => x.Name, x => x.Urls);
    }

        
    public async Task<string> Ping([FromServices]HttpClient httpClient, string targets)
    {
        var pong = string.Empty;
        if (!string.IsNullOrEmpty(targets))
        {
            // var httpClient = new HttpClient(new DiscoveryHttpClientHandler(_discoveryClient));
            _log.LogTrace($"Ping received. Remaining targets: {targets}");
            var allTargets = targets.Split(",").Where(x => x != _app.AppName).ToList();
                
            if (allTargets.Any())
            {
                var nextTarget = allTargets.First();
                var remainingTargets = string.Join(",", allTargets.Skip(1));
                try
                {
                    _log.LogInformation($"Sending ping request to {nextTarget}");
                    pong = await httpClient.GetStringAsync($"https://{nextTarget}/ServiceDiscovery/Ping?targets={remainingTargets}");
                }
                catch (Exception e)
                {
                    _log.LogInformation("test123");
                    _log.LogError(e, $"Call to {nextTarget} failed");
                    pong = $"{nextTarget} failed to answer";
                }
            }

        }
        return pong.Insert(0, $"Pong from {_app.AppName}\n");
    }
}