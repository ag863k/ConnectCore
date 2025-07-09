using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConnectCore.Shared.Services;

public interface IServiceDiscoveryClient
{
    Task<bool> RegisterServiceAsync(string serviceName, string serviceId, string host, int port, List<string>? tags = null);
    Task<bool> DeregisterServiceAsync(string serviceId);
    Task<List<ServiceInstance>> DiscoverServicesAsync(string serviceName);
    Task<ServiceInstance?> GetHealthyServiceInstanceAsync(string serviceName);
}

public class ServiceInstance
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsHealthy { get; set; } = true;
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
}

public class InMemoryServiceDiscoveryClient : IServiceDiscoveryClient
{
    private readonly Dictionary<string, List<ServiceInstance>> _services = new();
    private readonly ILogger<InMemoryServiceDiscoveryClient> _logger;

    public InMemoryServiceDiscoveryClient(ILogger<InMemoryServiceDiscoveryClient> logger)
    {
        _logger = logger;
    }

    public Task<bool> RegisterServiceAsync(string serviceName, string serviceId, string host, int port, List<string>? tags = null)
    {
        try
        {
            if (!_services.ContainsKey(serviceName))
            {
                _services[serviceName] = new List<ServiceInstance>();
            }

            var existingService = _services[serviceName].FirstOrDefault(s => s.ServiceId == serviceId);
            if (existingService != null)
            {
                existingService.Host = host;
                existingService.Port = port;
                existingService.Tags = tags ?? new List<string>();
                existingService.LastHealthCheck = DateTime.UtcNow;
                existingService.IsHealthy = true;
            }
            else
            {
                _services[serviceName].Add(new ServiceInstance
                {
                    ServiceName = serviceName,
                    ServiceId = serviceId,
                    Host = host,
                    Port = port,
                    Tags = tags ?? new List<string>(),
                    IsHealthy = true,
                    LastHealthCheck = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Service {ServiceName} with ID {ServiceId} registered at {Host}:{Port}", 
                serviceName, serviceId, host, port);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service {ServiceName} with ID {ServiceId}", serviceName, serviceId);
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeregisterServiceAsync(string serviceId)
    {
        try
        {
            foreach (var serviceList in _services.Values)
            {
                var serviceToRemove = serviceList.FirstOrDefault(s => s.ServiceId == serviceId);
                if (serviceToRemove != null)
                {
                    serviceList.Remove(serviceToRemove);
                    _logger.LogInformation("Service with ID {ServiceId} deregistered", serviceId);
                    return Task.FromResult(true);
                }
            }

            _logger.LogWarning("Service with ID {ServiceId} not found for deregistration", serviceId);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deregister service with ID {ServiceId}", serviceId);
            return Task.FromResult(false);
        }
    }

    public Task<List<ServiceInstance>> DiscoverServicesAsync(string serviceName)
    {
        try
        {
            if (_services.TryGetValue(serviceName, out var services))
            {
                var healthyServices = services.Where(s => s.IsHealthy).ToList();
                _logger.LogInformation("Discovered {Count} healthy instances of service {ServiceName}", 
                    healthyServices.Count, serviceName);
                return Task.FromResult(healthyServices);
            }

            _logger.LogInformation("No instances found for service {ServiceName}", serviceName);
            return Task.FromResult(new List<ServiceInstance>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover services for {ServiceName}", serviceName);
            return Task.FromResult(new List<ServiceInstance>());
        }
    }

    public Task<ServiceInstance?> GetHealthyServiceInstanceAsync(string serviceName)
    {
        try
        {
            if (_services.TryGetValue(serviceName, out var services))
            {
                var healthyServices = services.Where(s => s.IsHealthy).ToList();
                if (healthyServices.Any())
                {
                    var selectedService = healthyServices.OrderBy(s => s.LastHealthCheck).First();
                    selectedService.LastHealthCheck = DateTime.UtcNow;
                    return Task.FromResult<ServiceInstance?>(selectedService);
                }
            }

            _logger.LogWarning("No healthy instances found for service {ServiceName}", serviceName);
            return Task.FromResult<ServiceInstance?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get healthy service instance for {ServiceName}", serviceName);
            return Task.FromResult<ServiceInstance?>(null);
        }
    }
}
