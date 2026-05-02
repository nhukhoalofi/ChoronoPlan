using ChronoPlan.Domain.Entities;
using ChronoPlan.ViewModels;

namespace ChronoPlan.Services;

public interface IAppointmentService
{
    Task<AppointmentServiceResult> addAppointmentAsync(
        string userId,
        AppointmentCreateViewModel model);

    Task<AppointmentServiceResult> joinGroupMeetingAsync(string appointmentId, string userId);

    Task<AppointmentDetailsViewModel?> getAppointmentDetailsAsync(string appointmentId, string userId);

    Task<AppointmentServiceResult> updateAppointmentAsync(
        string appointmentId,
        string userId,
        AppointmentCreateViewModel model);

    Task<GroupMeeting?> findMatchingGroupMeetingAsync(
        string title,
        DateTime startTime,
        DateTime endTime,
        string userId);

    Task<AppointmentServiceResult> createPersonalAppointmentAsync(
        string userId,
        AppointmentCreateViewModel model);

    Task<AppointmentServiceResult> createGroupMeetingAsync(
        string userId,
        AppointmentCreateViewModel model);
}

public class AppointmentServiceResult
{
    public string Status { get; set; } = "Success";

    public string? Message { get; set; }

    public AppointmentListItemViewModel? ConflictAppointment { get; set; }

    public AppointmentListItemViewModel? MatchingGroupMeeting { get; set; }

    public string? MatchingGroupMeetingAppointmentId { get; set; }

    public static AppointmentServiceResult Success(string message)
    {
        return new AppointmentServiceResult
        {
            Status = "Success",
            Message = message
        };
    }

    public static AppointmentServiceResult Error(string message)
    {
        return new AppointmentServiceResult
        {
            Status = "Error",
            Message = message
        };
    }
}
