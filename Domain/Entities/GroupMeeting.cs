namespace ChronoPlan.Domain.Entities;

public class GroupMeeting : Appointment
{
    public ICollection<AppointmentParticipant> Participants { get; private set; }
        = new List<AppointmentParticipant>();

    public new static GroupMeeting Create(
        string calendarId,
        string title,
        string? location,
        DateTime startTime,
        DateTime endTime)
    {
        var meeting = new GroupMeeting();
        meeting.CalendarId = calendarId;
        meeting.UpdateDetails(title, location, startTime, endTime);

        return meeting;
    }

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

        Participants.Add(AppointmentParticipant.Create(AppointmentId, user.UserId));
    }
}
