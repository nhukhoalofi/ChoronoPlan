namespace ChronoPlan.ViewModels;

public class AppointmentListItemViewModel
{
    public string AppointmentId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Location { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsGroupMeeting { get; set; }

    public bool IsParticipant { get; set; }

    public int DayIndex { get; set; }

    public int TopPx { get; set; }

    public int HeightPx { get; set; }

    public string TimeText => $"{StartTime:HH:mm} - {EndTime:HH:mm}";
}
