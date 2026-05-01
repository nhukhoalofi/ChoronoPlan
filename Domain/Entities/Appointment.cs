namespace ChronoPlan.Domain.Entities;

public class Appointment
{
    public string AppointmentId { get; set; } = Guid.NewGuid().ToString("N");

    public string CalendarId { get; set; } = string.Empty;

    public Calendar? Calendar { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Location { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

    public TimeSpan GetDuration()
    {
        return EndTime - StartTime;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Title) && EndTime > StartTime;
    }

    public void AddReminder(Reminder reminder)
    {
        Reminders.Add(reminder);
    }

    public void RemoveReminder(Reminder reminder)
    {
        Reminders.Remove(reminder);
    }
}
