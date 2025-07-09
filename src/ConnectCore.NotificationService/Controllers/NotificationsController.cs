using Microsoft.AspNetCore.Mvc;
using ConnectCore.NotificationService.Services;
using ConnectCore.Shared.DTOs;
using ConnectCore.Shared.Models;

namespace ConnectCore.NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<NotificationDto>>> GetNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? type = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        NotificationType? notificationType = null;
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<NotificationType>(type, true, out var parsedType))
        {
            notificationType = parsedType;
        }

        var result = await _notificationService.GetNotificationsAsync(pageNumber, pageSize, userId, notificationType);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> GetNotification(Guid id)
    {
        var result = await _notificationService.GetNotificationByIdAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotificationsByUserId(Guid userId)
    {
        var result = await _notificationService.GetNotificationsByUserIdAsync(userId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> SendNotification([FromBody] SendNotificationDto sendNotificationDto)
    {
        var result = await _notificationService.SendNotificationAsync(sendNotificationDto);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetNotification), new { id = result.Data!.Id }, result);
    }

    [HttpPost("{id:guid}/mark-read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/mark-delivered")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsDelivered(Guid id)
    {
        var result = await _notificationService.MarkAsDeliveredAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult<ApiResponse<bool>>> RetryFailedNotification(Guid id)
    {
        var result = await _notificationService.RetryFailedNotificationAsync(id);
        
        if (!result.Success)
        {
            return result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, int>>>> GetNotificationStats([FromQuery] Guid? userId = null)
    {
        var result = await _notificationService.GetNotificationStatsAsync(userId);
        return Ok(result);
    }
}
