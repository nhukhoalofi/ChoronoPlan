using ChronoPlan.Data;
using ChronoPlan.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ChronoPlan.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .CountAsync();
    }

    public async Task<List<NotificationViewModel>> GetLatestNotificationsAsync(string userId, int take = 20)
    {
        return await _db.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new NotificationViewModel
            {
                NotificationId = x.NotificationId,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(string userId, string notificationId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);

        if (notification == null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        await _db.SaveChangesAsync();
    }
}
