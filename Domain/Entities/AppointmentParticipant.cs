namespace ChronoPlan.Domain.Entities;

public class AppointmentParticipant
{
    public string AppointmentId { get; set; } = string.Empty;

    public GroupMeeting? GroupMeeting { get; set; }

    public string UserId { get; set; } = string.Empty;

    public User? User { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
