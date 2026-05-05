namespace ChronoPlan.Domain.Entities;

public class Notification
{
    public string NotificationId { get; private set; } = Guid.NewGuid().ToString("N");

    public string UserId { get; private set; } = string.Empty;

    public User? User { get; set; }

    public string AppointmentId { get; private set; } = string.Empty;

    public Appointment? Appointment { get; set; }

    public string ReminderId { get; private set; } = string.Empty;

    public Reminder? Reminder { get; set; }

    public string Title { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public bool IsRead { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public static Notification Create(
        string userId,
        string appointmentId,
        string reminderId,
        string title,
        string message,
        DateTime createdAt)
    {
        return new Notification
        {
            UserId = userId,
            AppointmentId = appointmentId,
            ReminderId = reminderId,
            Title = title,
            Message = message,
            CreatedAt = createdAt,
            IsRead = false
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
