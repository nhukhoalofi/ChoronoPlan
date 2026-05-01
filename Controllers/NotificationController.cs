using ChronoPlan.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChronoPlan.Controllers;

public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetLatestNotificationsAsync(userId);
        return Json(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(string notificationId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await _notificationService.MarkAsReadAsync(userId, notificationId);
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
        return Json(new { unreadCount });
    }
}
