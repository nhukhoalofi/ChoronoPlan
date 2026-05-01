using ChronoPlan.ViewModels;

namespace ChronoPlan.Services;

public interface INotificationService
{
    Task<int> GetUnreadCountAsync(string userId);
    Task<List<NotificationViewModel>> GetLatestNotificationsAsync(string userId, int take = 20);
    Task MarkAsReadAsync(string userId, string notificationId);
}
