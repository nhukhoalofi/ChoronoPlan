namespace ChronoPlan.Domain.Entities;

public class Calendar
{
    public string CalendarId { get; set; } = Guid.NewGuid().ToString("N");

    public string UserId { get; set; } = string.Empty;

    public User? User { get; set; }

    public ICollection<Appointment> Appointments { get; set; }
        = new List<Appointment>();

    public void AddAppointment(Appointment appointment)
    {
        Appointments.Add(appointment);
    }

    public void RemoveAppointment(Appointment appointment)
    {
        Appointments.Remove(appointment);
    }
}
