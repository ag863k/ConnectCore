using ConnectCore.Shared.Models;
using ConnectCore.Shared.DTOs;
using ConnectCore.NotificationService.Data;
using Microsoft.EntityFrameworkCore;

namespace ConnectCore.NotificationService.Services;

public interface INotificationService
{
    Task<PagedResponse<NotificationDto>> GetNotificationsAsync(int pageNumber = 1, int pageSize = 10, Guid? userId = null, NotificationType? type = null);
    Task<ApiResponse<NotificationDto>> GetNotificationByIdAsync(Guid id);
    Task<ApiResponse<List<NotificationDto>>> GetNotificationsByUserIdAsync(Guid userId);
    Task<ApiResponse<NotificationDto>> SendNotificationAsync(SendNotificationDto sendNotificationDto);
    Task<ApiResponse<bool>> MarkAsReadAsync(Guid id);
    Task<ApiResponse<bool>> MarkAsDeliveredAsync(Guid id);
    Task<ApiResponse<bool>> RetryFailedNotificationAsync(Guid id);
    Task<ApiResponse<Dictionary<string, int>>> GetNotificationStatsAsync(Guid? userId = null);
}

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body);
}

public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message);
}

public interface IPushNotificationService
{
    Task<bool> SendPushNotificationAsync(string deviceToken, string title, string message);
}

public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        IPushNotificationService pushService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<PagedResponse<NotificationDto>> GetNotificationsAsync(int pageNumber = 1, int pageSize = 10, Guid? userId = null, NotificationType? type = null)
    {
        try
        {
            var query = _context.Notifications.Where(n => !n.IsDeleted);

            if (userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(n => n.Type == type.Value);
            }

            var totalCount = await query.CountAsync();
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => MapToDto(n))
                .ToListAsync();

            return PagedResponse<NotificationDto>.Create(notifications, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return new PagedResponse<NotificationDto>
            {
                Success = false,
                Message = "Error retrieving notifications"
            };
        }
    }

    public async Task<ApiResponse<NotificationDto>> GetNotificationByIdAsync(Guid id)
    {
        try
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            if (notification == null)
            {
                return ApiResponse<NotificationDto>.FailureResult("Notification not found");
            }

            return ApiResponse<NotificationDto>.SuccessResult(MapToDto(notification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification by ID {NotificationId}", id);
            return ApiResponse<NotificationDto>.FailureResult("Error retrieving notification");
        }
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetNotificationsByUserIdAsync(Guid userId)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => MapToDto(n))
                .ToListAsync();

            return ApiResponse<List<NotificationDto>>.SuccessResult(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return ApiResponse<List<NotificationDto>>.FailureResult("Error retrieving user notifications");
        }
    }

    public async Task<ApiResponse<NotificationDto>> SendNotificationAsync(SendNotificationDto sendNotificationDto)
    {
        try
        {
            // Parse notification type
            if (!Enum.TryParse<NotificationType>(sendNotificationDto.Type, true, out var notificationType))
            {
                return ApiResponse<NotificationDto>.FailureResult("Invalid notification type");
            }

            var notification = new Notification
            {
                UserId = sendNotificationDto.UserId,
                Title = sendNotificationDto.Title,
                Message = sendNotificationDto.Message,
                Type = notificationType,
                Status = NotificationStatus.Pending,
                Metadata = sendNotificationDto.Metadata,
                CreatedBy = "API"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send notification based on type
            bool sendResult = notificationType switch
            {
                NotificationType.Email => await SendEmailNotificationAsync(notification),
                NotificationType.SMS => await SendSmsNotificationAsync(notification),
                NotificationType.Push => await SendPushNotificationAsync(notification),
                NotificationType.InApp => await SendInAppNotificationAsync(notification),
                _ => false
            };

            // Update notification status
            notification.Status = sendResult ? NotificationStatus.Sent : NotificationStatus.Failed;
            notification.SentAt = sendResult ? DateTime.UtcNow : null;
            notification.UpdatedAt = DateTime.UtcNow;
            notification.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            var message = sendResult ? "Notification sent successfully" : "Failed to send notification";
            _logger.LogInformation("Notification {NotificationId} processing completed: {Result}", notification.Id, message);

            return ApiResponse<NotificationDto>.SuccessResult(MapToDto(notification), message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return ApiResponse<NotificationDto>.FailureResult("Error sending notification");
        }
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(Guid id)
    {
        try
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            if (notification == null)
            {
                return ApiResponse<bool>.FailureResult("Notification not found");
            }

            notification.Status = NotificationStatus.Read;
            notification.UpdatedAt = DateTime.UtcNow;
            notification.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification {NotificationId} marked as read", id);
            return ApiResponse<bool>.SuccessResult(true, "Notification marked as read");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read {NotificationId}", id);
            return ApiResponse<bool>.FailureResult("Error marking notification as read");
        }
    }

    public async Task<ApiResponse<bool>> MarkAsDeliveredAsync(Guid id)
    {
        try
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            if (notification == null)
            {
                return ApiResponse<bool>.FailureResult("Notification not found");
            }

            notification.Status = NotificationStatus.Delivered;
            notification.UpdatedAt = DateTime.UtcNow;
            notification.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification {NotificationId} marked as delivered", id);
            return ApiResponse<bool>.SuccessResult(true, "Notification marked as delivered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as delivered {NotificationId}", id);
            return ApiResponse<bool>.FailureResult("Error marking notification as delivered");
        }
    }

    public async Task<ApiResponse<bool>> RetryFailedNotificationAsync(Guid id)
    {
        try
        {
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
            if (notification == null)
            {
                return ApiResponse<bool>.FailureResult("Notification not found");
            }

            if (notification.Status != NotificationStatus.Failed)
            {
                return ApiResponse<bool>.FailureResult("Only failed notifications can be retried");
            }

            // Retry sending based on type
            bool sendResult = notification.Type switch
            {
                NotificationType.Email => await SendEmailNotificationAsync(notification),
                NotificationType.SMS => await SendSmsNotificationAsync(notification),
                NotificationType.Push => await SendPushNotificationAsync(notification),
                NotificationType.InApp => await SendInAppNotificationAsync(notification),
                _ => false
            };

            // Update notification status
            notification.Status = sendResult ? NotificationStatus.Sent : NotificationStatus.Failed;
            notification.SentAt = sendResult ? DateTime.UtcNow : notification.SentAt;
            notification.UpdatedAt = DateTime.UtcNow;
            notification.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            var message = sendResult ? "Notification retried successfully" : "Failed to retry notification";
            _logger.LogInformation("Notification {NotificationId} retry completed: {Result}", notification.Id, message);

            return ApiResponse<bool>.SuccessResult(sendResult, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying notification {NotificationId}", id);
            return ApiResponse<bool>.FailureResult("Error retrying notification");
        }
    }

    public async Task<ApiResponse<Dictionary<string, int>>> GetNotificationStatsAsync(Guid? userId = null)
    {
        try
        {
            var query = _context.Notifications.Where(n => !n.IsDeleted);

            if (userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId.Value);
            }

            var stats = await query
                .GroupBy(n => n.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = stats.ToDictionary(s => s.Status.ToString(), s => s.Count);

            return ApiResponse<Dictionary<string, int>>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification stats");
            return ApiResponse<Dictionary<string, int>>.FailureResult("Error retrieving notification stats");
        }
    }

    private async Task<bool> SendEmailNotificationAsync(Notification notification)
    {
        try
        {
            // In a real implementation, you would get user email from user service
            var userEmail = $"user-{notification.UserId}@example.com";
            return await _emailService.SendEmailAsync(userEmail, notification.Title, notification.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification {NotificationId}", notification.Id);
            return false;
        }
    }

    private async Task<bool> SendSmsNotificationAsync(Notification notification)
    {
        try
        {
            // In a real implementation, you would get user phone from user service
            var userPhone = "+1234567890";
            return await _smsService.SendSmsAsync(userPhone, notification.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS notification {NotificationId}", notification.Id);
            return false;
        }
    }

    private async Task<bool> SendPushNotificationAsync(Notification notification)
    {
        try
        {
            // In a real implementation, you would get user device token from user service
            var deviceToken = "mock-device-token";
            return await _pushService.SendPushNotificationAsync(deviceToken, notification.Title, notification.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification {NotificationId}", notification.Id);
            return false;
        }
    }

    private async Task<bool> SendInAppNotificationAsync(Notification notification)
    {
        try
        {
            // In-app notifications are just stored in the database and marked as sent
            await Task.Delay(10); // Simulate processing
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending in-app notification {NotificationId}", notification.Id);
            return false;
        }
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToString(),
            Status = notification.Status.ToString(),
            CreatedAt = notification.CreatedAt,
            SentAt = notification.SentAt
        };
    }
}

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        await Task.Delay(100);
        _logger.LogInformation("Mock Email sent to {To}: {Subject}", to, subject);
        return true;
    }
}

public class MockSmsService : ISmsService
{
    private readonly ILogger<MockSmsService> _logger;

    public MockSmsService(ILogger<MockSmsService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        await Task.Delay(50);
        _logger.LogInformation("Mock SMS sent to {PhoneNumber}: {Message}", phoneNumber, message);
        return true;
    }
}

public class MockPushNotificationService : IPushNotificationService
{
    private readonly ILogger<MockPushNotificationService> _logger;

    public MockPushNotificationService(ILogger<MockPushNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string message)
    {
        await Task.Delay(25);
        _logger.LogInformation("Mock Push notification sent to {DeviceToken}: {Title} - {Message}", deviceToken, title, message);
        return true;
    }
}
