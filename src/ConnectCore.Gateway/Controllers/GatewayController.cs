using Microsoft.AspNetCore.Mvc;
using ConnectCore.Shared.Services;
using ConnectCore.Shared.DTOs;

namespace ConnectCore.Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly IServiceDiscoveryClient _serviceDiscovery;
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(IServiceDiscoveryClient serviceDiscovery, ILogger<GatewayController> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<object>>> GetGatewayStatus()
    {
        try
        {
            var serviceHealthChecks = new Dictionary<string, object>();

            var services = new[] { "user-service", "product-service", "order-service", "notification-service" };
            
            foreach (var serviceName in services)
            {
                var instances = await _serviceDiscovery.DiscoverServicesAsync(serviceName);
                serviceHealthChecks[serviceName] = new
                {
                    InstanceCount = instances.Count,
                    HealthyInstances = instances.Count(i => i.IsHealthy),
                    LastChecked = DateTime.UtcNow
                };
            }

            var gatewayStatus = new
            {
                Gateway = "ConnectCore API Gateway",
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Services = serviceHealthChecks,
                Version = "1.0.0"
            };

            return Ok(ApiResponse<object>.SuccessResult(gatewayStatus));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gateway status");
            return StatusCode(500, ApiResponse<object>.FailureResult("Error retrieving gateway status"));
        }
    }

    [HttpGet("services")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, List<ServiceInstance>>>>> GetRegisteredServices()
    {
        try
        {
            var allServices = new Dictionary<string, List<ServiceInstance>>();
            var serviceNames = new[] { "user-service", "product-service", "order-service", "notification-service" };

            foreach (var serviceName in serviceNames)
            {
                var instances = await _serviceDiscovery.DiscoverServicesAsync(serviceName);
                allServices[serviceName] = instances;
            }

            return Ok(ApiResponse<Dictionary<string, List<ServiceInstance>>>.SuccessResult(allServices));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registered services");
            return StatusCode(500, ApiResponse<Dictionary<string, List<ServiceInstance>>>.FailureResult("Error retrieving registered services"));
        }
    }

    [HttpGet("routes")]
    public ActionResult<ApiResponse<object>> GetAvailableRoutes()
    {
        try
        {
            var routes = new
            {
                UserService = new
                {
                    BaseUrl = "/api/users",
                    Endpoints = new[]
                    {
                        "GET /api/users - Get paginated users",
                        "GET /api/users/{id} - Get user by ID",
                        "GET /api/users/by-email/{email} - Get user by email",
                        "POST /api/users - Create new user",
                        "PUT /api/users/{id} - Update user",
                        "DELETE /api/users/{id} - Delete user",
                        "POST /api/users/{id}/activate - Activate user",
                        "POST /api/users/{id}/deactivate - Deactivate user"
                    }
                },
                ProductService = new
                {
                    BaseUrl = "/api/products",
                    Endpoints = new[]
                    {
                        "GET /api/products - Get paginated products",
                        "GET /api/products/{id} - Get product by ID",
                        "GET /api/products/by-sku/{sku} - Get product by SKU",
                        "POST /api/products - Create new product",
                        "PUT /api/products/{id} - Update product",
                        "DELETE /api/products/{id} - Delete product",
                        "POST /api/products/{id}/stock - Update product stock",
                        "GET /api/products/{id}/stock-availability - Check stock availability",
                        "GET /api/products/categories - Get all categories"
                    }
                },
                OrderService = new
                {
                    BaseUrl = "/api/orders",
                    Endpoints = new[]
                    {
                        "GET /api/orders - Get paginated orders",
                        "GET /api/orders/{id} - Get order by ID",
                        "GET /api/orders/user/{userId} - Get orders by user ID",
                        "POST /api/orders - Create new order",
                        "PUT /api/orders/{id}/status - Update order status",
                        "POST /api/orders/{id}/cancel - Cancel order",
                        "POST /api/orders/{id}/ship - Ship order",
                        "POST /api/orders/{id}/deliver - Deliver order"
                    }
                },
                NotificationService = new
                {
                    BaseUrl = "/api/notifications",
                    Endpoints = new[]
                    {
                        "GET /api/notifications - Get paginated notifications",
                        "GET /api/notifications/{id} - Get notification by ID",
                        "GET /api/notifications/user/{userId} - Get notifications by user ID",
                        "POST /api/notifications - Send new notification",
                        "POST /api/notifications/{id}/mark-read - Mark notification as read",
                        "POST /api/notifications/{id}/mark-delivered - Mark notification as delivered",
                        "POST /api/notifications/{id}/retry - Retry failed notification",
                        "GET /api/notifications/stats - Get notification statistics"
                    }
                },
                ServiceDiscovery = new
                {
                    BaseUrl = "/api/servicediscovery",
                    Endpoints = new[]
                    {
                        "POST /api/servicediscovery/register - Register service",
                        "DELETE /api/servicediscovery/deregister/{serviceId} - Deregister service",
                        "GET /api/servicediscovery/discover/{serviceName} - Discover service instances",
                        "GET /api/servicediscovery/instance/{serviceName} - Get healthy service instance",
                        "GET /api/servicediscovery/services - Get all services"
                    }
                },
                Gateway = new
                {
                    BaseUrl = "/api/gateway",
                    Endpoints = new[]
                    {
                        "GET /api/gateway/status - Get gateway status",
                        "GET /api/gateway/services - Get registered services",
                        "GET /api/gateway/routes - Get available routes"
                    }
                }
            };

            return Ok(ApiResponse<object>.SuccessResult(routes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available routes");
            return StatusCode(500, ApiResponse<object>.FailureResult("Error retrieving available routes"));
        }
    }
}
