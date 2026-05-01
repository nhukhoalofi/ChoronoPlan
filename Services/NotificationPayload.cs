namespace ChronoPlan.Services;

public class NotificationPayload
{
    public string NotificationId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int UnreadCount { get; set; }
}
