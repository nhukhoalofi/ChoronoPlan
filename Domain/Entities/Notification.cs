namespace ChronoPlan.Domain.Entities;

public class Notification
{
    public string NotificationId { get; set; } = Guid.NewGuid().ToString("N");

    public string UserId { get; set; } = string.Empty;

    public User? User { get; set; }

    public string AppointmentId { get; set; } = string.Empty;

    public Appointment? Appointment { get; set; }

    public string ReminderId { get; set; } = string.Empty;

    public Reminder? Reminder { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
