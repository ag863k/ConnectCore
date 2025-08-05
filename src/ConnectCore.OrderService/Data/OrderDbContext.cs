using Microsoft.EntityFrameworkCore;
using ConnectCore.Shared.Models;
using System.Text.Json;

namespace ConnectCore.OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Items)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<OrderItem>>(v, (JsonSerializerOptions?)null) ?? new List<OrderItem>());
            
            entity.OwnsOne(e => e.ShippingAddress, address =>
            {
                address.Property(a => a.Street).HasMaxLength(100);
                address.Property(a => a.City).HasMaxLength(50);
                address.Property(a => a.State).HasMaxLength(50);
                address.Property(a => a.PostalCode).HasMaxLength(20);
                address.Property(a => a.Country).HasMaxLength(50);
            });
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });

        var orders = GenerateSeedOrders();
        modelBuilder.Entity<Order>().HasData(orders);
    }

    private static Order[] GenerateSeedOrders()
    {
        return new[]
        {
            new Order
            {
                Id = Guid.Parse("11110000-1111-1111-1111-111111111111"),
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Items = new List<OrderItem>
                {
                    new() { ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ProductName = "Laptop Pro 15", Quantity = 1, UnitPrice = 1299.99m },
                    new() { ProductId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), ProductName = "Wireless Mouse", Quantity = 1, UnitPrice = 29.99m }
                },
                TotalAmount = 1329.98m,
                Status = OrderStatus.Delivered,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ShippedAt = DateTime.UtcNow.AddDays(-8),
                DeliveredAt = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "System"
            },
            new Order
            {
                Id = Guid.Parse("22220000-2222-2222-2222-222222222222"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                Items = new List<OrderItem>
                {
                    new() { ProductId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), ProductName = "Mechanical Keyboard", Quantity = 1, UnitPrice = 149.99m },
                    new() { ProductId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), ProductName = "4K Monitor", Quantity = 1, UnitPrice = 399.99m }
                },
                TotalAmount = 549.98m,
                Status = OrderStatus.Shipped,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ShippedAt = DateTime.UtcNow.AddDays(-2),
                CreatedBy = "System"
            },
            new Order
            {
                Id = Guid.Parse("33330000-3333-3333-3333-333333333333"),
                UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"), 
                Items = new List<OrderItem>
                {
                    new() { ProductId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), ProductName = "Office Chair", Quantity = 1, UnitPrice = 249.99m }
                },
                TotalAmount = 249.99m,
                Status = OrderStatus.Processing,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                CreatedBy = "System"
            }
        };
    }
}
