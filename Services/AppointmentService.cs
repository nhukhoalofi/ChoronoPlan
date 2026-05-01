using ChronoPlan.Data;
using ChronoPlan.Domain.Entities;
using ChronoPlan.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ChronoPlan.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _db;

    public AppointmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AppointmentServiceResult> CreateAppointmentAsync(
        string userId,
        AppointmentCreateViewModel model)
    {
        var calendar = await _db.Calendars
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (calendar == null)
        {
            return AppointmentServiceResult.Error("Không tìm thấy calendar của người dùng.");
        }

        var start = model.StartTime;
        var end = model.EndTime;

        if (model.Choice == "Reschedule" && model.NewStartTime.HasValue && model.NewEndTime.HasValue)
        {
            start = model.NewStartTime.Value;
            end = model.NewEndTime.Value;
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return AppointmentServiceResult.Error("Tên cuộc hẹn không được để trống.");
        }

        if (end <= start)
        {
            return AppointmentServiceResult.Error("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
        }

        var conflict = await FindConflictAsync(userId, start, end);

        if (conflict != null && model.Choice != "Replace")
        {
            return new AppointmentServiceResult
            {
                Status = "Conflict",
                Message = "Bạn đã có cuộc hẹn vào thời gian này.",
                ConflictAppointment = ToListItem(conflict)
            };
        }

        if (conflict != null && model.Choice == "Replace")
        {
            var ownConflict = await _db.Appointments
                .Include(x => x.Calendar)
                .FirstOrDefaultAsync(x =>
                    x.AppointmentId == conflict.AppointmentId &&
                    x.Calendar != null &&
                    x.Calendar.UserId == userId);

            if (ownConflict == null)
            {
                return AppointmentServiceResult.Error("Bạn chỉ có thể replace cuộc hẹn thuộc calendar của bạn.");
            }

            _db.Appointments.Remove(ownConflict);
        }

        if (model.MatchingMeetingId != null && model.Choice == "Join")
        {
            var meeting = await _db.GroupMeetings
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.AppointmentId == model.MatchingMeetingId);

            if (meeting == null)
            {
                return AppointmentServiceResult.Error("Không tìm thấy group meeting.");
            }

            var alreadyJoined = meeting.Participants.Any(x => x.UserId == userId);
            if (!alreadyJoined)
            {
                meeting.Participants.Add(new AppointmentParticipant
                {
                    AppointmentId = meeting.AppointmentId,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            return AppointmentServiceResult.Success("Bạn đã tham gia group meeting.");
        }

        if (string.IsNullOrWhiteSpace(model.Choice))
        {
            var matchingMeeting = await FindMatchingGroupMeetingAsync(
                userId,
                model.Title,
                end - start
            );

            if (matchingMeeting != null)
            {
                return new AppointmentServiceResult
                {
                    Status = "GroupMeetingFound",
                    Message = "Có một group meeting giống cuộc hẹn bạn đang tạo.",
                    MatchingGroupMeeting = ToListItem(matchingMeeting)
                };
            }
        }

        var appointment = new Appointment
        {
            CalendarId = calendar.CalendarId,
            Title = model.Title.Trim(),
            Location = model.Location?.Trim(),
            StartTime = start,
            EndTime = end
        };

        if (model.ReminderMinutesBefore.HasValue)
        {
            appointment.Reminders.Add(new Reminder
            {
                ReminderTime = start.AddMinutes(-model.ReminderMinutesBefore.Value),
                Type = "Popup",
                Message = $"Reminder for {model.Title}"
            });
        }

        _db.Appointments.Add(appointment);

        await _db.SaveChangesAsync();

        return AppointmentServiceResult.Success("Tạo cuộc hẹn thành công.");
    }

    private async Task<Appointment?> FindConflictAsync(string userId, DateTime start, DateTime end)
    {
        var ownAppointments = await _db.Appointments
            .Include(x => x.Calendar)
            .Where(x => x.Calendar != null && x.Calendar.UserId == userId)
            .ToListAsync();

        var joinedMeetings = await _db.GroupMeetings
            .Include(x => x.Participants)
            .Where(x => x.Participants.Any(p => p.UserId == userId))
            .ToListAsync();

        var busyAppointments = ownAppointments
            .Concat(joinedMeetings.Cast<Appointment>())
            .DistinctBy(x => x.AppointmentId)
            .ToList();

        return busyAppointments.FirstOrDefault(x =>
            start < x.EndTime && end > x.StartTime
        );
    }

    private async Task<GroupMeeting?> FindMatchingGroupMeetingAsync(
        string userId,
        string title,
        TimeSpan duration)
    {
        var durationMinutes = (int)duration.TotalMinutes;
        var normalizedTitle = title.Trim().ToLower();

        return await _db.GroupMeetings
            .Include(x => x.Participants)
            .Where(x => !x.Participants.Any(p => p.UserId == userId))
            .FirstOrDefaultAsync(x =>
                x.Title.ToLower() == normalizedTitle &&
                EF.Functions.DateDiffMinute(x.StartTime, x.EndTime) == durationMinutes
            );
    }

    private static AppointmentListItemViewModel ToListItem(Appointment appointment)
    {
        return new AppointmentListItemViewModel
        {
            AppointmentId = appointment.AppointmentId,
            Title = appointment.Title,
            Location = appointment.Location,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            IsGroupMeeting = appointment is GroupMeeting
        };
    }
}
