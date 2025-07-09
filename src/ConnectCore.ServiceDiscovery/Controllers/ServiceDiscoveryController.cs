using Microsoft.AspNetCore.Mvc;
using ConnectCore.Shared.Services;
using ConnectCore.Shared.DTOs;

namespace ConnectCore.ServiceDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceDiscoveryController : ControllerBase
{
    private readonly IServiceDiscoveryClient _serviceDiscovery;
    private readonly ILogger<ServiceDiscoveryController> _logger;

    public ServiceDiscoveryController(IServiceDiscoveryClient serviceDiscovery, ILogger<ServiceDiscoveryController> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<string>>> RegisterService([FromBody] ServiceRegistrationDto registration)
    {
        try
        {
            var success = await _serviceDiscovery.RegisterServiceAsync(
                registration.ServiceName,
                registration.ServiceId,
                registration.Host,
                registration.Port,
                registration.Tags);

            if (success)
            {
                _logger.LogInformation("Service {ServiceName} registered successfully", registration.ServiceName);
                return Ok(ApiResponse<string>.SuccessResult("Service registered successfully"));
            }

            return BadRequest(ApiResponse<string>.FailureResult("Failed to register service"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering service {ServiceName}", registration.ServiceName);
            return StatusCode(500, ApiResponse<string>.FailureResult("Internal server error"));
        }
    }

    [HttpDelete("deregister/{serviceId}")]
    public async Task<ActionResult<ApiResponse<string>>> DeregisterService(string serviceId)
    {
        try
        {
            var success = await _serviceDiscovery.DeregisterServiceAsync(serviceId);

            if (success)
            {
                _logger.LogInformation("Service {ServiceId} deregistered successfully", serviceId);
                return Ok(ApiResponse<string>.SuccessResult("Service deregistered successfully"));
            }

            return NotFound(ApiResponse<string>.FailureResult("Service not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deregistering service {ServiceId}", serviceId);
            return StatusCode(500, ApiResponse<string>.FailureResult("Internal server error"));
        }
    }

    [HttpGet("discover/{serviceName}")]
    public async Task<ActionResult<ApiResponse<List<ServiceInstance>>>> DiscoverServices(string serviceName)
    {
        try
        {
            var services = await _serviceDiscovery.DiscoverServicesAsync(serviceName);
            return Ok(ApiResponse<List<ServiceInstance>>.SuccessResult(services));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering services for {ServiceName}", serviceName);
            return StatusCode(500, ApiResponse<List<ServiceInstance>>.FailureResult("Internal server error"));
        }
    }

    [HttpGet("instance/{serviceName}")]
    public async Task<ActionResult<ApiResponse<ServiceInstance>>> GetServiceInstance(string serviceName)
    {
        try
        {
            var serviceInstance = await _serviceDiscovery.GetHealthyServiceInstanceAsync(serviceName);
            
            if (serviceInstance != null)
            {
                return Ok(ApiResponse<ServiceInstance>.SuccessResult(serviceInstance));
            }

            return NotFound(ApiResponse<ServiceInstance>.FailureResult("No healthy service instances found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service instance for {ServiceName}", serviceName);
            return StatusCode(500, ApiResponse<ServiceInstance>.FailureResult("Internal server error"));
        }
    }

    [HttpGet("services")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, int>>>> GetAllServices()
    {
        try
        {
            var allServices = new Dictionary<string, int>();
            
            var userServices = await _serviceDiscovery.DiscoverServicesAsync("user-service");
            var productServices = await _serviceDiscovery.DiscoverServicesAsync("product-service");
            var orderServices = await _serviceDiscovery.DiscoverServicesAsync("order-service");
            var notificationServices = await _serviceDiscovery.DiscoverServicesAsync("notification-service");

            allServices["user-service"] = userServices.Count;
            allServices["product-service"] = productServices.Count;
            allServices["order-service"] = orderServices.Count;
            allServices["notification-service"] = notificationServices.Count;

            return Ok(ApiResponse<Dictionary<string, int>>.SuccessResult(allServices));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all services");
            return StatusCode(500, ApiResponse<Dictionary<string, int>>.FailureResult("Internal server error"));
        }
    }
}
