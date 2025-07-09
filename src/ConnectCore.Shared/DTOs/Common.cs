namespace ConnectCore.Shared.DTOs;

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<string> Errors { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? RequestId { get; init; }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> FailureResult(string error, List<string>? additionalErrors = null)
        => new() { Success = false, Message = error, Errors = additionalErrors ?? new List<string>() };

    public static ApiResponse<T> FailureResult(List<string> errors)
        => new() { Success = false, Errors = errors };
}

public record PagedResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;

    public static PagedResponse<T> Create(IEnumerable<T> data, int pageNumber, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<T>
        {
            Success = true,
            Data = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}

public record UserDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public string Status { get; init; } = string.Empty;
    public AddressDto? Address { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateUserDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public AddressDto? Address { get; init; }
}

public record UpdateUserDto
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public AddressDto? Address { get; init; }
}

public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Category { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public string SKU { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record CreateProductDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Category { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public string SKU { get; init; } = string.Empty;
}

public record UpdateProductDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public string? Category { get; init; }
    public int? StockQuantity { get; init; }
}

public record OrderDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public AddressDto? ShippingAddress { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
}

public record CreateOrderDto
{
    public Guid UserId { get; init; }
    public List<CreateOrderItemDto> Items { get; init; } = new();
    public AddressDto? ShippingAddress { get; init; }
}

public record OrderItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}

public record CreateOrderItemDto
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

public record NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? SentAt { get; init; }
}

public record SendNotificationDto
{
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record AddressDto
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}

public record ServiceRegistrationDto
{
    public string ServiceName { get; init; } = string.Empty;
    public string ServiceId { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public List<string> Tags { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
}
