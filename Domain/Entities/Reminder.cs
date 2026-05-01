namespace ChronoPlan.Domain.Entities;

public class Reminder
{
    public string ReminderId { get; set; } = Guid.NewGuid().ToString("N");

    public string AppointmentId { get; set; } = string.Empty;

    public Appointment? Appointment { get; set; }

    public DateTime ReminderTime { get; set; }

    public string Type { get; set; } = "Popup";

    public string? Message { get; set; }

    public bool IsCanceled { get; set; }

    public void Trigger()
    {
    }

    public void Cancel()
    {
        IsCanceled = true;
    }
}
