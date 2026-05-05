namespace ChronoPlan.Domain.Entities;

public class Appointment
{
    public string AppointmentId { get; protected set; } = Guid.NewGuid().ToString("N");

    public string CalendarId { get; protected set; } = string.Empty;

    public Calendar? Calendar { get; set; }

    public string Title { get; protected set; } = string.Empty;

    public string? Location { get; protected set; }

    public DateTime StartTime { get; protected set; }

    public DateTime EndTime { get; protected set; }

    public ICollection<Reminder> Reminders { get; private set; } = new List<Reminder>();

    public static Appointment Create(
        string calendarId,
        string title,
        string? location,
        DateTime startTime,
        DateTime endTime)
    {
        var appointment = new Appointment();
        appointment.CalendarId = calendarId;
        appointment.UpdateDetails(title, location, startTime, endTime);

        return appointment;
    }

    public void UpdateDetails(string title, string? location, DateTime startTime, DateTime endTime)
    {
        Title = title;
        Location = location;
        StartTime = startTime;
        EndTime = endTime;
    }

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

    public void ClearReminders()
    {
        Reminders.Clear();
    }

    public void RemoveReminder(Reminder reminder)
    {
        Reminders.Remove(reminder);
    }
}
