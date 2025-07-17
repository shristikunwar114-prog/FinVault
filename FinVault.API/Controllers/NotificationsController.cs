using System.Security.Claims;
using FinVault.API.DTOs.Common;
using FinVault.API.Models;
using FinVault.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinVault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        var userId = GetUserId();
        var result = await _notificationService.GetNotificationsAsync(userId, unreadOnly);
        return Ok(ApiResponse<List<Notification>>.Ok(result));
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = GetUserId();
        await _notificationService.MarkReadAsync(userId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Notification marked as read."));
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetUserId();
        await _notificationService.MarkAllReadAsync(userId);
        return Ok(ApiResponse<object>.Ok(null!, "All notifications marked as read."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        await _notificationService.DeleteAsync(userId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Notification deleted."));
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim!);
    }
}
