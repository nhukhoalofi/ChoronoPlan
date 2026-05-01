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

        var validationError = ValidateAppointmentInput(model.Title, start, end);
        if (validationError != null)
        {
            return AppointmentServiceResult.Error(validationError);
        }

        if (IsJoinAction(model))
        {
            var appointmentId = model.MatchingGroupMeetingAppointmentId ?? model.MatchingMeetingId;
            if (string.IsNullOrWhiteSpace(appointmentId))
            {
                return AppointmentServiceResult.Error("Không tìm thấy group meeting cần tham gia.");
            }

            return await JoinGroupMeetingAsync(appointmentId, userId);
        }

        if (model.Choice != "Replace")
        {
            var matchingMeeting = await FindMatchingGroupMeetingAsync(
                model.Title,
                start,
                end,
                userId);

            if (matchingMeeting != null)
            {
                var alreadyJoined = matchingMeeting.Participants.Any(x => x.UserId == userId);
                if (alreadyJoined)
                {
                    return new AppointmentServiceResult
                    {
                        Status = "AlreadyJoined",
                        Message = "You have already joined this group meeting.",
                        MatchingGroupMeeting = ToListItem(matchingMeeting),
                        MatchingGroupMeetingAppointmentId = matchingMeeting.AppointmentId
                    };
                }

                return new AppointmentServiceResult
                {
                    Status = "MatchingGroupMeetingFound",
                    Message = "A group meeting with the same title and time already exists. Do you want to join it?",
                    MatchingGroupMeeting = ToListItem(matchingMeeting),
                    MatchingGroupMeetingAppointmentId = matchingMeeting.AppointmentId
                };
            }
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

        return model.IsGroupMeeting
            ? await CreateGroupMeetingAsync(userId, model, calendar.CalendarId, start, end)
            : await CreatePersonalAppointmentAsync(userId, model, calendar.CalendarId, start, end);
    }

    public async Task<GroupMeeting?> FindMatchingGroupMeetingAsync(
        string title,
        DateTime startTime,
        DateTime endTime,
        string userId)
    {
        var normalizedTitle = title.Trim().ToLower();

        return await _db.GroupMeetings
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x =>
                x.Title.ToLower() == normalizedTitle &&
                x.StartTime == startTime &&
                x.EndTime == endTime);
    }

    public async Task<AppointmentServiceResult> CreatePersonalAppointmentAsync(
        string userId,
        AppointmentCreateViewModel model)
    {
        var calendar = await _db.Calendars.FirstOrDefaultAsync(x => x.UserId == userId);
        if (calendar == null)
        {
            return AppointmentServiceResult.Error("Không tìm thấy calendar của người dùng.");
        }

        return await CreatePersonalAppointmentAsync(userId, model, calendar.CalendarId, model.StartTime, model.EndTime);
    }

    public async Task<AppointmentServiceResult> CreateGroupMeetingAsync(
        string userId,
        AppointmentCreateViewModel model)
    {
        var calendar = await _db.Calendars.FirstOrDefaultAsync(x => x.UserId == userId);
        if (calendar == null)
        {
            return AppointmentServiceResult.Error("Không tìm thấy calendar của người dùng.");
        }

        return await CreateGroupMeetingAsync(userId, model, calendar.CalendarId, model.StartTime, model.EndTime);
    }

    public async Task<AppointmentServiceResult> JoinGroupMeetingAsync(string appointmentId, string userId)
    {
        var meeting = await _db.GroupMeetings
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId);

        if (meeting == null)
        {
            return AppointmentServiceResult.Error("Không tìm thấy group meeting.");
        }

        var alreadyJoined = meeting.Participants.Any(x => x.UserId == userId);
        if (alreadyJoined)
        {
            return AppointmentServiceResult.Success("You have already joined this group meeting.");
        }

        meeting.Participants.Add(new AppointmentParticipant
        {
            AppointmentId = meeting.AppointmentId,
            UserId = userId,
            JoinedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return AppointmentServiceResult.Success("Joined group meeting successfully.");
    }

    public async Task<AppointmentDetailsViewModel?> GetAppointmentDetailsAsync(string appointmentId, string userId)
    {
        var appointment = await _db.Appointments
            .Include(x => x.Calendar)
            .Include(x => x.Reminders)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId);

        if (appointment == null)
        {
            return null;
        }

        var isOwner = appointment.Calendar?.UserId == userId;
        var isParticipant = await _db.AppointmentParticipants
            .AsNoTracking()
            .AnyAsync(x => x.AppointmentId == appointmentId && x.UserId == userId);

        if (!isOwner && !isParticipant)
        {
            return null;
        }

        var isGroupMeeting = await _db.GroupMeetings
            .AsNoTracking()
            .AnyAsync(x => x.AppointmentId == appointmentId);

        var participants = isGroupMeeting
            ? await _db.AppointmentParticipants
                .AsNoTracking()
                .Where(x => x.AppointmentId == appointmentId)
                .Join(
                    _db.Users.AsNoTracking(),
                    participant => participant.UserId,
                    user => user.UserId,
                    (participant, user) => new AppointmentParticipantViewModel
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email,
                        JoinedAt = participant.JoinedAt
                    })
                .OrderBy(x => x.JoinedAt)
                .ToListAsync()
            : new List<AppointmentParticipantViewModel>();

        return new AppointmentDetailsViewModel
        {
            AppointmentId = appointment.AppointmentId,
            Title = appointment.Title,
            Location = appointment.Location,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            IsGroupMeeting = isGroupMeeting,
            CanEdit = isOwner,
            ReminderType = appointment.Reminders.FirstOrDefault()?.Type ?? ReminderType.Popup,
            ReminderMinutesBefore = appointment.Reminders
                .Where(x => x.ReminderTime <= appointment.StartTime)
                .Select(x => (int)(appointment.StartTime - x.ReminderTime).TotalMinutes)
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToList(),
            Participants = participants
        };
    }

    public async Task<AppointmentServiceResult> UpdateAppointmentAsync(
        string appointmentId,
        string userId,
        AppointmentCreateViewModel model)
    {
        var appointment = await _db.Appointments
            .Include(x => x.Calendar)
            .Include(x => x.Reminders)
            .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId);

        if (appointment == null)
        {
            return AppointmentServiceResult.Error("Không tìm thấy appointment.");
        }

        if (appointment.Calendar?.UserId != userId)
        {
            return AppointmentServiceResult.Error("Bạn không có quyền chỉnh sửa appointment này.");
        }

        var validationError = ValidateAppointmentInput(model.Title, model.StartTime, model.EndTime);
        if (validationError != null)
        {
            return AppointmentServiceResult.Error(validationError);
        }

        var conflict = await FindConflictAsync(userId, model.StartTime, model.EndTime, appointmentId);
        if (conflict != null)
        {
            return new AppointmentServiceResult
            {
                Status = "Conflict",
                Message = "Bạn đã có cuộc hẹn vào thời gian này.",
                ConflictAppointment = ToListItem(conflict)
            };
        }

        var currentIsGroupMeeting = await _db.GroupMeetings
            .AnyAsync(x => x.AppointmentId == appointmentId);

        if (model.IsGroupMeeting != currentIsGroupMeeting)
        {
            return AppointmentServiceResult.Error("Không thể đổi loại appointment trong màn hình chỉnh sửa.");
        }

        if (currentIsGroupMeeting)
        {
            var matchingMeeting = await FindMatchingGroupMeetingAsync(
                model.Title,
                model.StartTime,
                model.EndTime,
                userId);

            if (matchingMeeting != null && matchingMeeting.AppointmentId != appointmentId)
            {
                return new AppointmentServiceResult
                {
                    Status = "MatchingGroupMeetingFound",
                    Message = "A group meeting with the same title and time already exists. Do you want to join it?",
                    MatchingGroupMeeting = ToListItem(matchingMeeting),
                    MatchingGroupMeetingAppointmentId = matchingMeeting.AppointmentId
                };
            }
        }

        appointment.Title = model.Title.Trim();
        appointment.Location = model.Location?.Trim();
        appointment.StartTime = model.StartTime;
        appointment.EndTime = model.EndTime;

        _db.Reminders.RemoveRange(appointment.Reminders);
        AddReminders(appointment, model, model.StartTime);

        await _db.SaveChangesAsync();

        return AppointmentServiceResult.Success(
            currentIsGroupMeeting ? "Group meeting updated successfully." : "Appointment updated successfully.");
    }

    private async Task<AppointmentServiceResult> CreatePersonalAppointmentAsync(
        string userId,
        AppointmentCreateViewModel model,
        string calendarId,
        DateTime start,
        DateTime end)
    {
        var appointment = new Appointment
        {
            CalendarId = calendarId,
            Title = model.Title.Trim(),
            Location = model.Location?.Trim(),
            StartTime = start,
            EndTime = end
        };

        AddReminders(appointment, model, start);

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        return AppointmentServiceResult.Success("Appointment created successfully.");
    }

    private async Task<AppointmentServiceResult> CreateGroupMeetingAsync(
        string userId,
        AppointmentCreateViewModel model,
        string calendarId,
        DateTime start,
        DateTime end)
    {
        var meeting = new GroupMeeting
        {
            CalendarId = calendarId,
            Title = model.Title.Trim(),
            Location = model.Location?.Trim(),
            StartTime = start,
            EndTime = end
        };

        AddReminders(meeting, model, start);

        meeting.Participants.Add(new AppointmentParticipant
        {
            AppointmentId = meeting.AppointmentId,
            UserId = userId,
            JoinedAt = DateTime.Now
        });

        _db.GroupMeetings.Add(meeting);
        await _db.SaveChangesAsync();

        return AppointmentServiceResult.Success("Group meeting created successfully.");
    }

    private static void AddReminders(Appointment appointment, AppointmentCreateViewModel model, DateTime start)
    {
        var reminderType = NormalizeReminderType(model.ReminderType);

        foreach (var minutes in model.ReminderMinutesBefore.Distinct().Where(x => x > 0))
        {
            appointment.Reminders.Add(new Reminder
            {
                ReminderTime = start.AddMinutes(-minutes),
                Type = reminderType,
                Message = $"Reminder for {model.Title}",
                IsCanceled = false,
                IsSent = false
            });
        }
    }

    private static string? ValidateAppointmentInput(string title, DateTime start, DateTime end)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Tên cuộc hẹn không được để trống.";
        }

        if (end <= start)
        {
            return "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.";
        }

        return null;
    }

    private static bool IsJoinAction(AppointmentCreateViewModel model)
    {
        return model.GroupMeetingAction == "Join" || model.Choice == "Join";
    }

    private async Task<Appointment?> FindConflictAsync(
        string userId,
        DateTime start,
        DateTime end,
        string? excludeAppointmentId = null)
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
            x.AppointmentId != excludeAppointmentId &&
            start < x.EndTime && end > x.StartTime
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

    private static string NormalizeReminderType(string? reminderType)
    {
        return reminderType switch
        {
            ReminderType.Email => ReminderType.Email,
            ReminderType.Both => ReminderType.Both,
            _ => ReminderType.Popup
        };
    }
}
