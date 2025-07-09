using ConnectCore.Shared.Models;
using ConnectCore.Shared.DTOs;
using ConnectCore.OrderService.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ConnectCore.OrderService.Services;

public interface IOrderService
{
    Task<PagedResponse<OrderDto>> GetOrdersAsync(int pageNumber = 1, int pageSize = 10, Guid? userId = null);
    Task<ApiResponse<OrderDto>> GetOrderByIdAsync(Guid id);
    Task<ApiResponse<List<OrderDto>>> GetOrdersByUserIdAsync(Guid userId);
    Task<ApiResponse<OrderDto>> CreateOrderAsync(CreateOrderDto createOrderDto);
    Task<ApiResponse<bool>> UpdateOrderStatusAsync(Guid id, OrderStatus status);
    Task<ApiResponse<bool>> CancelOrderAsync(Guid id);
    Task<ApiResponse<bool>> ShipOrderAsync(Guid id);
    Task<ApiResponse<bool>> DeliverOrderAsync(Guid id);
}

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly HttpClient _httpClient;

    public OrderService(OrderDbContext context, ILogger<OrderService> logger, HttpClient httpClient)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<PagedResponse<OrderDto>> GetOrdersAsync(int pageNumber = 1, int pageSize = 10, Guid? userId = null)
    {
        try
        {
            var query = _context.Orders.Where(o => !o.IsDeleted);

            if (userId.HasValue)
            {
                query = query.Where(o => o.UserId == userId.Value);
            }

            var totalCount = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => MapToDto(o))
                .ToListAsync();

            return PagedResponse<OrderDto>.Create(orders, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return new PagedResponse<OrderDto>
            {
                Success = false,
                Message = "Error retrieving orders"
            };
        }
    }

    public async Task<ApiResponse<OrderDto>> GetOrderByIdAsync(Guid id)
    {
        try
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (order == null)
            {
                return ApiResponse<OrderDto>.FailureResult("Order not found");
            }

            return ApiResponse<OrderDto>.SuccessResult(MapToDto(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order by ID {OrderId}", id);
            return ApiResponse<OrderDto>.FailureResult("Error retrieving order");
        }
    }

    public async Task<ApiResponse<List<OrderDto>>> GetOrdersByUserIdAsync(Guid userId)
    {
        try
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId && !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => MapToDto(o))
                .ToListAsync();

            return ApiResponse<List<OrderDto>>.SuccessResult(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
            return ApiResponse<List<OrderDto>>.FailureResult("Error retrieving user orders");
        }
    }

    public async Task<ApiResponse<OrderDto>> CreateOrderAsync(CreateOrderDto createOrderDto)
    {
        try
        {
            var userExists = await ValidateUserExistsAsync(createOrderDto.UserId);
            if (!userExists)
            {
                return ApiResponse<OrderDto>.FailureResult("User not found");
            }

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var item in createOrderDto.Items)
            {
                var productValidation = await ValidateProductAndCalculatePrice(item);
                if (!productValidation.IsValid)
                {
                    return ApiResponse<OrderDto>.FailureResult($"Product validation failed: {productValidation.ErrorMessage}");
                }

                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = productValidation.ProductName!,
                    Quantity = item.Quantity,
                    UnitPrice = productValidation.UnitPrice
                };

                orderItems.Add(orderItem);
                totalAmount += orderItem.TotalPrice;
            }

            var order = new Order
            {
                UserId = createOrderDto.UserId,
                Items = orderItems,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending,
                ShippingAddress = createOrderDto.ShippingAddress != null ? new Address
                {
                    Street = createOrderDto.ShippingAddress.Street,
                    City = createOrderDto.ShippingAddress.City,
                    State = createOrderDto.ShippingAddress.State,
                    PostalCode = createOrderDto.ShippingAddress.PostalCode,
                    Country = createOrderDto.ShippingAddress.Country
                } : null,
                CreatedBy = "API"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Reserve stock (simulate call to product service)
            foreach (var item in createOrderDto.Items)
            {
                await ReserveProductStockAsync(item.ProductId, item.Quantity);
            }

            _logger.LogInformation("Order created successfully with ID {OrderId}", order.Id);
            return ApiResponse<OrderDto>.SuccessResult(MapToDto(order), "Order created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return ApiResponse<OrderDto>.FailureResult("Error creating order");
        }
    }

    public async Task<ApiResponse<bool>> UpdateOrderStatusAsync(Guid id, OrderStatus status)
    {
        try
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (order == null)
            {
                return ApiResponse<bool>.FailureResult("Order not found");
            }

            var previousStatus = order.Status;
            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = "API";

            // Set status-specific timestamps
            switch (status)
            {
                case OrderStatus.Shipped when previousStatus != OrderStatus.Shipped:
                    order.ShippedAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Delivered when previousStatus != OrderStatus.Delivered:
                    order.DeliveredAt = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order status updated successfully for ID {OrderId}: {PreviousStatus} -> {NewStatus}", 
                id, previousStatus, status);
            return ApiResponse<bool>.SuccessResult(true, $"Order status updated to {status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for {OrderId}", id);
            return ApiResponse<bool>.FailureResult("Error updating order status");
        }
    }

    public async Task<ApiResponse<bool>> CancelOrderAsync(Guid id)
    {
        try
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (order == null)
            {
                return ApiResponse<bool>.FailureResult("Order not found");
            }

            if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            {
                return ApiResponse<bool>.FailureResult("Cannot cancel shipped or delivered orders");
            }

            // Release reserved stock
            foreach (var item in order.Items)
            {
                await ReleaseProductStockAsync(item.ProductId, item.Quantity);
            }

            return await UpdateOrderStatusAsync(id, OrderStatus.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return ApiResponse<bool>.FailureResult("Error cancelling order");
        }
    }

    public async Task<ApiResponse<bool>> ShipOrderAsync(Guid id)
    {
        try
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (order == null)
            {
                return ApiResponse<bool>.FailureResult("Order not found");
            }

            if (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.Processing)
            {
                return ApiResponse<bool>.FailureResult($"Cannot ship order with status: {order.Status}");
            }

            return await UpdateOrderStatusAsync(id, OrderStatus.Shipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping order {OrderId}", id);
            return ApiResponse<bool>.FailureResult("Error shipping order");
        }
    }

    public async Task<ApiResponse<bool>> DeliverOrderAsync(Guid id)
    {
        try
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (order == null)
            {
                return ApiResponse<bool>.FailureResult("Order not found");
            }

            if (order.Status != OrderStatus.Shipped)
            {
                return ApiResponse<bool>.FailureResult($"Cannot deliver order with status: {order.Status}");
            }

            return await UpdateOrderStatusAsync(id, OrderStatus.Delivered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering order {OrderId}", id);
            return ApiResponse<bool>.FailureResult("Error delivering order");
        }
    }

    private async Task<bool> ValidateUserExistsAsync(Guid userId)
    {
        try
        {
            await Task.Delay(10);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user {UserId}", userId);
            return false;
        }
    }

    private async Task<ProductValidationResult> ValidateProductAndCalculatePrice(CreateOrderItemDto item)
    {
        try
        {
            await Task.Delay(10);
            
            var mockProducts = new Dictionary<Guid, (string Name, decimal Price, int Stock)>
            {
                { Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ("Laptop Pro 15", 1299.99m, 50) },
                { Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), ("Wireless Mouse", 29.99m, 200) },
                { Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), ("Mechanical Keyboard", 149.99m, 75) },
                { Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), ("4K Monitor", 399.99m, 30) },
                { Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), ("Office Chair", 249.99m, 25) }
            };

            if (mockProducts.TryGetValue(item.ProductId, out var product))
            {
                if (product.Stock < item.Quantity)
                {
                    return new ProductValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Insufficient stock for product {product.Name}. Available: {product.Stock}, Requested: {item.Quantity}"
                    };
                }

                return new ProductValidationResult
                {
                    IsValid = true,
                    ProductName = product.Name,
                    UnitPrice = product.Price
                };
            }

            return new ProductValidationResult
            {
                IsValid = false,
                ErrorMessage = "Product not found"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating product {ProductId}", item.ProductId);
            return new ProductValidationResult
            {
                IsValid = false,
                ErrorMessage = "Error validating product"
            };
        }
    }

    private async Task<bool> ReserveProductStockAsync(Guid productId, int quantity)
    {
        try
        {
            // Simulate call to Product Service to reserve stock
            await Task.Delay(10); // Simulate network call
            _logger.LogInformation("Reserved {Quantity} units of product {ProductId}", quantity, productId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for product {ProductId}", productId);
            return false;
        }
    }

    private async Task<bool> ReleaseProductStockAsync(Guid productId, int quantity)
    {
        try
        {
            // Simulate call to Product Service to release stock
            await Task.Delay(10); // Simulate network call
            _logger.LogInformation("Released {Quantity} units of product {ProductId}", quantity, productId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing stock for product {ProductId}", productId);
            return false;
        }
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            ShippingAddress = order.ShippingAddress != null ? new AddressDto
            {
                Street = order.ShippingAddress.Street,
                City = order.ShippingAddress.City,
                State = order.ShippingAddress.State,
                PostalCode = order.ShippingAddress.PostalCode,
                Country = order.ShippingAddress.Country
            } : null,
            CreatedAt = order.CreatedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt
        };
    }
}

public class ProductValidationResult
{
    public bool IsValid { get; set; }
    public string? ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ErrorMessage { get; set; }
}
