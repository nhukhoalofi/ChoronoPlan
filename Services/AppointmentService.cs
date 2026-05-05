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

    public async Task<AppointmentServiceResult> addAppointmentAsync(
        string userId,
        AppointmentCreateViewModel model)
    {
        var calendar = await _db.Calendars
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (calendar == null)
        {
            return AppointmentServiceResult.Error("User's calendar not found.");
        }

        var start = model.StartTime;
        var end = model.EndTime;

        if (model.Choice == "Reschedule" && model.NewStartTime.HasValue && model.NewEndTime.HasValue)
        {
            start = model.NewStartTime.Value;
            end = model.NewEndTime.Value;
        }

        var validationError = validateInput(model.Title, start, end);
        if (validationError != null)
        {
            return AppointmentServiceResult.Error(validationError);
        }

        if (IsJoinAction(model))
        {
            var appointmentId = model.MatchingGroupMeetingAppointmentId ?? model.MatchingMeetingId;
            if (string.IsNullOrWhiteSpace(appointmentId))
            {
                return AppointmentServiceResult.Error("Group meeting to join not found.");
            }

            return await joinGroupMeetingAsync(appointmentId, userId);
        }

        if (model.Choice != "Replace")
        {
            var matchingMeeting = await findMatchingGroupMeetingAsync(
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

        var conflict = await checkConflictAsync(userId, start, end);

        if (conflict != null && model.Choice != "Replace")
        {
            return new AppointmentServiceResult
            {
                Status = "Conflict",
                Message = "You already have an appointment at this time.",
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
                return AppointmentServiceResult.Error("You can only replace appointments belonging to your calendar.");
            }

            _db.Appointments.Remove(ownConflict);
        }

        return model.IsGroupMeeting
                ? await createGroupMeetingAsync(userId, model, calendar.CalendarId, start, end)
                : await createPersonalAppointmentAsync(userId, model, calendar.CalendarId, start, end);
    }

            public async Task<GroupMeeting?> findMatchingGroupMeetingAsync(
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

    public async Task<AppointmentServiceResult> createPersonalAppointmentAsync(
        string userId,
        AppointmentCreateViewModel model)
    {
        var calendar = await _db.Calendars.FirstOrDefaultAsync(x => x.UserId == userId);
        if (calendar == null)
        {
            return AppointmentServiceResult.Error("User's calendar not found.");
        }

        return await createPersonalAppointmentAsync(userId, model, calendar.CalendarId, model.StartTime, model.EndTime);
    }

    public async Task<AppointmentServiceResult> createGroupMeetingAsync(
        string userId,
        AppointmentCreateViewModel model)
    {
        var calendar = await _db.Calendars.FirstOrDefaultAsync(x => x.UserId == userId);
        if (calendar == null)
        {
            return AppointmentServiceResult.Error("Không tìm thấy calendar của người dùng.");
        }

        return await createGroupMeetingAsync(userId, model, calendar.CalendarId, model.StartTime, model.EndTime);
    }

    public async Task<AppointmentServiceResult> joinGroupMeetingAsync(string appointmentId, string userId)
    {
        var meeting = await _db.GroupMeetings
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.AppointmentId == appointmentId);

        if (meeting == null)
        {
            return AppointmentServiceResult.Error("Group meeting not found.");
        }

        var alreadyJoined = meeting.Participants.Any(x => x.UserId == userId);
        if (alreadyJoined)
        {
            return AppointmentServiceResult.Success("You have already joined this group meeting.");
        }

        var user = await _db.Users.FirstAsync(x => x.UserId == userId);
        meeting.AddParticipant(user);

        await _db.SaveChangesAsync();

        return AppointmentServiceResult.Success("Joined group meeting successfully.");
    }

    public async Task<AppointmentDetailsViewModel?> getAppointmentDetailsAsync(string appointmentId, string userId)
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

    public async Task<AppointmentServiceResult> updateAppointmentAsync(
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
            return AppointmentServiceResult.Error("Appointment not found.");
        }

        if (appointment.Calendar?.UserId != userId)
        {
            return AppointmentServiceResult.Error("You don't have permission to edit this appointment.");
        }

        var validationError = validateInput(model.Title, model.StartTime, model.EndTime);
        if (validationError != null)
        {
            return AppointmentServiceResult.Error(validationError);
        }

        var conflict = await checkConflictAsync(userId, model.StartTime, model.EndTime, appointmentId);
        if (conflict != null)
        {
            return new AppointmentServiceResult
            {
                Status = "Conflict",
                Message = "You already have an appointment at this time.",
                ConflictAppointment = ToListItem(conflict)
            };
        }

        var currentIsGroupMeeting = await _db.GroupMeetings
            .AnyAsync(x => x.AppointmentId == appointmentId);

        if (model.IsGroupMeeting != currentIsGroupMeeting)
        {
            return AppointmentServiceResult.Error("Cannot change appointment type in the edit screen.");
        }

        if (currentIsGroupMeeting)
        {
            var matchingMeeting = await findMatchingGroupMeetingAsync(
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

        appointment.UpdateDetails(
            model.Title.Trim(),
            model.Location?.Trim(),
            model.StartTime,
            model.EndTime);

        _db.Reminders.RemoveRange(appointment.Reminders);
        appointment.ClearReminders();
        addReminderAsync(appointment, model, model.StartTime);

        await _db.SaveChangesAsync();

        return AppointmentServiceResult.Success(
            currentIsGroupMeeting ? "Group meeting updated successfully." : "Appointment updated successfully.");
    }

    private async Task<AppointmentServiceResult> createPersonalAppointmentAsync(
        string userId,
        AppointmentCreateViewModel model,
        string calendarId,
        DateTime start,
        DateTime end)
    {
        var appointment = Appointment.Create(
            calendarId,
            model.Title.Trim(),
            model.Location?.Trim(),
            start,
            end);

        addReminderAsync(appointment, model, start);

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        return AppointmentServiceResult.Success("Appointment created successfully.");
    }

    private async Task<AppointmentServiceResult> createGroupMeetingAsync(
        string userId,
        AppointmentCreateViewModel model,
        string calendarId,
        DateTime start,
        DateTime end)
    {
        var meeting = GroupMeeting.Create(
            calendarId,
            model.Title.Trim(),
            model.Location?.Trim(),
            start,
            end);

        addReminderAsync(meeting, model, start);

        var user = await _db.Users.FirstAsync(x => x.UserId == userId);
        meeting.AddParticipant(user);

        _db.GroupMeetings.Add(meeting);
        await _db.SaveChangesAsync();

        return AppointmentServiceResult.Success("Group meeting created successfully.");
    }

    private static void addReminderAsync(Appointment appointment, AppointmentCreateViewModel model, DateTime start)
    {
        var reminderType = NormalizeReminderType(model.ReminderType);

        foreach (var minutes in model.ReminderMinutesBefore.Distinct().Where(x => x > 0))
        {
            appointment.AddReminder(Reminder.Create(
                appointment.AppointmentId,
                start.AddMinutes(-minutes),
                reminderType,
                $"Reminder for {model.Title}"));
        }
    }

    private static string? validateInput(string title, DateTime start, DateTime end)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Appointment title must not be empty.";
        }

        if (end <= start)
        {
            return "End time must be later than start time.";
        }

        return null;
    }

    private static bool IsJoinAction(AppointmentCreateViewModel model)
    {
        return model.GroupMeetingAction == "Join" || model.Choice == "Join";
    }

    private async Task<Appointment?> checkConflictAsync(
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
