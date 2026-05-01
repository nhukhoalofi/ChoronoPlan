namespace ChronoPlan.ViewModels;

public class AppointmentDetailsViewModel
{
    public string AppointmentId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Location { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsGroupMeeting { get; set; }

    public bool CanEdit { get; set; }

    public string ReminderType { get; set; } = ChronoPlan.Services.ReminderType.Popup;

    public List<int> ReminderMinutesBefore { get; set; } = new();

    public List<AppointmentParticipantViewModel> Participants { get; set; } = new();
}

public class AppointmentParticipantViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }
}
