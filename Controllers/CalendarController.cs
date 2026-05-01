using ChronoPlan.Data;
using ChronoPlan.Domain.Entities;
using ChronoPlan.Services;
using ChronoPlan.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChronoPlan.Controllers;

public class CalendarController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAppointmentService _appointmentService;
    private readonly INotificationService _notificationService;

    public CalendarController(AppDbContext db, IAppointmentService appointmentService, INotificationService notificationService)
    {
        _db = db;
        _appointmentService = appointmentService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? weekStart, string? q)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var model = await BuildCalendarPageAsync(userId, weekStart, q);
        model.SuccessMessage = TempData["Success"]?.ToString();
        model.ErrorMessage = TempData["Error"]?.ToString();

        model.UnreadNotificationCount = await _notificationService.GetUnreadCountAsync(userId);
        model.Notifications = await _notificationService.GetLatestNotificationsAsync(userId);

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppointmentCreateViewModel form)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _appointmentService.CreateAppointmentAsync(userId, form);

        if (result.Status == "Success")
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index), new { weekStart = NormalizeWeekStart(form.WeekStart ?? form.StartTime).ToString("yyyy-MM-dd") });
        }

        if (result.Status == "AlreadyJoined")
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index), new { weekStart = NormalizeWeekStart(form.WeekStart ?? form.StartTime).ToString("yyyy-MM-dd") });
        }

        var page = await BuildCalendarPageAsync(userId, form.WeekStart ?? form.StartTime, null);
        page.Form = form;
        page.Form.WeekStart = page.WeekStart;
        page.ShowCreateModal = true;
        page.ErrorMessage = result.Message;
        page.ConflictAppointment = result.ConflictAppointment;
        page.MatchingGroupMeeting = result.MatchingGroupMeeting;
        page.Form.MatchingGroupMeetingAppointmentId = result.MatchingGroupMeetingAppointmentId
            ?? result.MatchingGroupMeeting?.AppointmentId;

        return View("Index", page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinGroupMeeting(string appointmentId, DateTime? weekStart)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _appointmentService.JoinGroupMeetingAsync(appointmentId, userId);

        if (result.Status == "Success")
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new
        {
            weekStart = NormalizeWeekStart(weekStart ?? DateTime.Today).ToString("yyyy-MM-dd")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAppointment(string appointmentId, AppointmentCreateViewModel form)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _appointmentService.UpdateAppointmentAsync(appointmentId, userId, form);

        if (result.Status == "Success")
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new
        {
            weekStart = NormalizeWeekStart(form.WeekStart ?? form.StartTime).ToString("yyyy-MM-dd")
        });
    }

    [HttpGet]
    public async Task<IActionResult> AppointmentDetails(string id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var details = await _appointmentService.GetAppointmentDetailsAsync(id, userId);
        if (details == null)
        {
            return NotFound();
        }

        return Json(details);
    }

    private string? GetCurrentUserId()
    {
        return HttpContext.Session.GetString("UserId");
    }

    private static DateTime NormalizeWeekStart(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }

    private async Task<CalendarPageViewModel> BuildCalendarPageAsync(
        string userId,
        DateTime? requestedWeekStart,
        string? searchQuery)
    {
        var weekStart = NormalizeWeekStart(requestedWeekStart ?? DateTime.Today);
        var weekEnd = weekStart.AddDays(7);

        var ownQuery = _db.Appointments
            .Include(x => x.Calendar)
            .Where(x =>
                x.Calendar != null &&
                x.Calendar.UserId == userId &&
                x.StartTime < weekEnd &&
                x.EndTime > weekStart);

        var joinedQuery = _db.GroupMeetings
            .Include(x => x.Participants)
            .Where(x =>
                x.Participants.Any(p => p.UserId == userId) &&
                x.StartTime < weekEnd &&
                x.EndTime > weekStart);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var q = searchQuery.Trim().ToLower();

            ownQuery = ownQuery.Where(x =>
                x.Title.ToLower().Contains(q) ||
                (x.Location != null && x.Location.ToLower().Contains(q)));

            joinedQuery = joinedQuery.Where(x =>
                x.Title.ToLower().Contains(q) ||
                (x.Location != null && x.Location.ToLower().Contains(q)));
        }

        var ownAppointments = await ownQuery
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var joinedMeetings = await joinedQuery
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var joinedMeetingIds = joinedMeetings
            .Select(x => x.AppointmentId)
            .ToHashSet();

        var allAppointments = ownAppointments
            .Concat(joinedMeetings.Cast<Appointment>())
            .DistinctBy(x => x.AppointmentId)
            .OrderBy(x => x.StartTime)
            .Select(x =>
            {
                var dayIndex = (x.StartTime.Date - weekStart).Days;

                const int startHour = 1;
                const int pixelsPerHour = 48;

                var startMinutesFrom8Am =
                    ((x.StartTime.Hour - startHour) * 60) + x.StartTime.Minute;

                var durationMinutes =
                    Math.Max(30, (int)(x.EndTime - x.StartTime).TotalMinutes);

                return new AppointmentListItemViewModel
                {
                    AppointmentId = x.AppointmentId,
                    Title = x.Title,
                    Location = x.Location,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    IsGroupMeeting = x is GroupMeeting,
                    IsParticipant = joinedMeetingIds.Contains(x.AppointmentId),
                    DayIndex = dayIndex,
                    TopPx = Math.Max(0, startMinutesFrom8Am * pixelsPerHour / 60),
                    HeightPx = Math.Max(32, durationMinutes * pixelsPerHour / 60)
                };
            })
            .Where(x => x.DayIndex >= 0 && x.DayIndex <= 6)
            .ToList();

        var defaultStart = DateTime.Today.AddHours(DateTime.Now.Hour + 1);
        var defaultEnd = defaultStart.AddHours(1);

        if (defaultStart < weekStart || defaultStart >= weekEnd)
        {
            defaultStart = weekStart.AddHours(9);
            defaultEnd = weekStart.AddHours(10);
        }

        return new CalendarPageViewModel
        {
            WeekStart = weekStart,

            Days = Enumerable.Range(0, 7)
                .Select(i => weekStart.AddDays(i))
                .ToList(),

            TimeSlots = Enumerable.Range(1, 23)
                .Select(h =>
                {
                    if (h < 12) return $"{h} AM";
                    if (h == 12) return "12 PM";
                    return $"{h - 12} PM";
                })
                .ToList(),

            Appointments = allAppointments,
            SearchQuery = searchQuery,

            Form = new AppointmentCreateViewModel
            {
                StartTime = defaultStart,
                EndTime = defaultEnd,
                WeekStart = weekStart
            }
        };
    }
}


