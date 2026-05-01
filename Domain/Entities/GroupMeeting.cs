namespace ChronoPlan.Domain.Entities;

public class GroupMeeting : Appointment
{
    public ICollection<AppointmentParticipant> Participants { get; set; }
        = new List<AppointmentParticipant>();

    public bool IsMatch(string title, TimeSpan duration)
    {
        return string.Equals(Title.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase)
               && GetDuration() == duration;
    }

    public void AddParticipant(User user)
    {
        if (Participants.Any(p => p.UserId == user.UserId))
        {
            return;
        }

        Participants.Add(new AppointmentParticipant
        {
            AppointmentId = AppointmentId,
            UserId = user.UserId,
            JoinedAt = DateTime.UtcNow
        });
    }
}
