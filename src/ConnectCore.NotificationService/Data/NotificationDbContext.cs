using Microsoft.EntityFrameworkCore;
using ConnectCore.Shared.Models;
using System.Text.Json;

namespace ConnectCore.NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
        });

        var notifications = GenerateSeedNotifications();
        modelBuilder.Entity<Notification>().HasData(notifications);
    }

    private static Notification[] GenerateSeedNotifications()
    {
        return new[]
        {
            new Notification
            {
                Id = Guid.Parse("11111000-1111-1111-1111-111111111111"),
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // John Doe
                Title = "Order Confirmed",
                Message = "Your order #11110000-1111-1111-1111-111111111111 has been confirmed and is being processed.",
                Type = NotificationType.Email,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow.AddDays(-10),
                Metadata = new Dictionary<string, object> { { "orderId", "11110000-1111-1111-1111-111111111111" } },
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CreatedBy = "System"
            },
            new Notification
            {
                Id = Guid.Parse("22222000-2222-2222-2222-222222222222"),
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // John Doe
                Title = "Order Shipped",
                Message = "Good news! Your order has been shipped and is on its way to you.",
                Type = NotificationType.SMS,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow.AddDays(-8),
                Metadata = new Dictionary<string, object> { { "orderId", "11110000-1111-1111-1111-111111111111" }, { "trackingNumber", "TRK123456789" } },
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                CreatedBy = "System"
            },
            new Notification
            {
                Id = Guid.Parse("33333000-3333-3333-3333-333333333333"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Jane Smith
                Title = "Welcome to ConnectCore!",
                Message = "Welcome to ConnectCore! We're excited to have you on board. Explore our products and start shopping.",
                Type = NotificationType.Email,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow.AddDays(-25),
                Metadata = new Dictionary<string, object> { { "notificationType", "welcome" } },
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                CreatedBy = "System"
            },
            new Notification
            {
                Id = Guid.Parse("44444000-4444-4444-4444-444444444444"),
                UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"), // Bob Johnson
                Title = "Order Processing",
                Message = "Your order is currently being processed. We'll notify you once it ships.",
                Type = NotificationType.Push,
                Status = NotificationStatus.Pending,
                Metadata = new Dictionary<string, object> { { "orderId", "33330000-3333-3333-3333-333333333333" } },
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                CreatedBy = "System"
            }
        };
    }
}
