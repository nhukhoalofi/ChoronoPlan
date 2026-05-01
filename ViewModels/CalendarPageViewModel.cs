namespace ChronoPlan.ViewModels;

public class CalendarPageViewModel
{
    public List<AppointmentListItemViewModel> Appointments { get; set; } = new();

    public AppointmentCreateViewModel Form { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public string? SuccessMessage { get; set; }

    public AppointmentListItemViewModel? ConflictAppointment { get; set; }

    public AppointmentListItemViewModel? MatchingGroupMeeting { get; set; }

    public bool ShowCreateModal { get; set; }

    public DateTime WeekStart { get; set; }

    public DateTime WeekEnd => WeekStart.AddDays(7);

    public List<DateTime> Days { get; set; } = new();

    public List<string> TimeSlots { get; set; } = new();

    public string? SearchQuery { get; set; }

    public DateTime PreviousWeekStart => WeekStart.AddDays(-7);

    public DateTime NextWeekStart => WeekStart.AddDays(7);

    public bool ShowCurrentTimeLine { get; set; }

    public int CurrentTimeDayIndex { get; set; }

    public int CurrentTimeTopPx { get; set; }
}
