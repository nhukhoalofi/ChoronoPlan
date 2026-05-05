namespace ChronoPlan.Domain.Entities;

public class Calendar
{
    public string CalendarId { get; protected set; } = Guid.NewGuid().ToString("N");

    public string UserId { get; protected set; } = string.Empty;

    public User? User { get; set; }

    public ICollection<Appointment> Appointments { get; private set; }
        = new List<Appointment>();

    public static Calendar Create(string userId)
    {
        return new Calendar
        {
            UserId = userId
        };
    }

    public void AddAppointment(Appointment appointment)
    {
        Appointments.Add(appointment);
    }

    public void RemoveAppointment(Appointment appointment)
    {
        Appointments.Remove(appointment);
    }
}
