namespace ChronoPlan.Domain.Entities;

public class AppointmentParticipant
{
    public string AppointmentId { get; protected set; } = string.Empty;

    public GroupMeeting? GroupMeeting { get; set; }

    public string UserId { get; protected set; } = string.Empty;

    public User? User { get; set; }

    public DateTime JoinedAt { get; protected set; } = DateTime.UtcNow;

    public static AppointmentParticipant Create(string appointmentId, string userId, DateTime? joinedAt = null)
    {
        return new AppointmentParticipant
        {
            AppointmentId = appointmentId,
            UserId = userId,
            JoinedAt = joinedAt ?? DateTime.UtcNow
        };
    }
}
