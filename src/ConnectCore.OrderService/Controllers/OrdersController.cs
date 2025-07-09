using Microsoft.AspNetCore.Mvc;
using ConnectCore.OrderService.Services;
using ConnectCore.Shared.DTOs;
using ConnectCore.Shared.Models;

namespace ConnectCore.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetOrders(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? userId = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _orderService.GetOrdersAsync(pageNumber, pageSize, userId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(Guid id)
    {
        var result = await _orderService.GetOrderByIdAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrdersByUserId(Guid userId)
    {
        var result = await _orderService.GetOrdersByUserIdAsync(userId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        var result = await _orderService.CreateOrderAsync(createOrderDto);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetOrder), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            return BadRequest(ApiResponse<bool>.FailureResult("Invalid order status"));
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, status);
        
        if (!result.Success)
        {
            return result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelOrder(Guid id)
    {
        var result = await _orderService.CancelOrderAsync(id);
        
        if (!result.Success)
        {
            return result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/ship")]
    public async Task<ActionResult<ApiResponse<bool>>> ShipOrder(Guid id)
    {
        var result = await _orderService.ShipOrderAsync(id);
        
        if (!result.Success)
        {
            return result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/deliver")]
    public async Task<ActionResult<ApiResponse<bool>>> DeliverOrder(Guid id)
    {
        var result = await _orderService.DeliverOrderAsync(id);
        
        if (!result.Success)
        {
            return result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }
}

public record UpdateOrderStatusRequest(string Status);
