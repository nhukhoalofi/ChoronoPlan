namespace ChronoPlan.Domain.Entities;

public class Reminder
{
    public string ReminderId { get; private set; } = Guid.NewGuid().ToString("N");

    public string AppointmentId { get; private set; } = string.Empty;

    public Appointment? Appointment { get; set; }

    public DateTime ReminderTime { get; private set; }

    public string Type { get; private set; } = "Popup";

    public string? Message { get; private set; }

    public bool IsCanceled { get; private set; }

    public bool IsSent { get; private set; }

    public DateTime? SentAt { get; private set; }

    public static Reminder Create(
        string appointmentId,
        DateTime reminderTime,
        string type,
        string? message,
        bool isCanceled = false,
        bool isSent = false,
        DateTime? sentAt = null)
    {
        return new Reminder
        {
            AppointmentId = appointmentId,
            ReminderTime = reminderTime,
            Type = type,
            Message = message,
            IsCanceled = isCanceled,
            IsSent = isSent,
            SentAt = sentAt
        };
    }

    public void MarkAsSent(DateTime sentAt)
    {
        IsSent = true;
        SentAt = sentAt;
    }

    public void Trigger()
    {
    }

    public void Cancel()
    {
        IsCanceled = true;
    }
}
