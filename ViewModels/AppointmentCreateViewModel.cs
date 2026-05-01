using System.ComponentModel.DataAnnotations;

namespace ChronoPlan.ViewModels;

public class AppointmentCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }

    [Required]
    public DateTime StartTime { get; set; } = DateTime.Today.AddHours(9);

    [Required]
    public DateTime EndTime { get; set; } = DateTime.Today.AddHours(10);

    public List<int> ReminderMinutesBefore { get; set; } = new();

    public string ReminderType { get; set; } = ChronoPlan.Services.ReminderType.Popup;

    public bool IsGroupMeeting { get; set; }

    public string? MatchingGroupMeetingAppointmentId { get; set; }

    public string? GroupMeetingAction { get; set; }

    public string? Choice { get; set; }

    public DateTime? NewStartTime { get; set; }

    public DateTime? NewEndTime { get; set; }

    public string? MatchingMeetingId { get; set; }

    public DateTime? WeekStart { get; set; }
}
