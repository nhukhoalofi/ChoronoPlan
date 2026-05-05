using ChronoPlan.Data;
using ChronoPlan.Domain.Entities;
using ChronoPlan.Hubs;
using ChronoPlan.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChronoPlan.BackgroundServices;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationHub> hubContext,
        ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing due reminders.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var now = DateTime.Now;

        var reminders = await db.Reminders
            .Include(r => r.Appointment!)
                .ThenInclude(a => a.Calendar!)
                    .ThenInclude(c => c.User)
            .Where(r => !r.IsCanceled && !r.IsSent && r.ReminderTime <= now)
            .ToListAsync(stoppingToken);

        if (!reminders.Any())
        {
            return;
        }

        var pendingPayloads = new List<PendingNotificationPayload>();

        foreach (var reminder in reminders)
        {
            if (reminder.Appointment == null)
            {
                reminder.MarkAsSent(now);
                continue;
            }

            var recipients = await GetRecipientsAsync(db, reminder.Appointment, stoppingToken);
            var message = reminder.Message ?? $"Reminder: {reminder.Appointment.Title}";
            var title = reminder.Appointment.Title;

            foreach (var recipient in recipients)
            {
                var notification = Notification.Create(
                    recipient.UserId,
                    reminder.Appointment.AppointmentId,
                    reminder.ReminderId,
                    title,
                    message,
                    now);

                db.Notifications.Add(notification);

                if (reminder.Type == ReminderType.Email || reminder.Type == ReminderType.Both)
                {
                    try
                    {
                        var emailBody = BuildEmailBody(reminder.Appointment, message);
                        await emailSender.SendAsync(recipient.Email, $"Reminder: {title}", emailBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to send reminder email. ReminderId: {ReminderId}, UserId: {UserId}",
                            reminder.ReminderId,
                            recipient.UserId);
                    }
                }

                pendingPayloads.Add(new PendingNotificationPayload
                {
                    UserId = recipient.UserId,
                    Notification = notification
                });
            }

            reminder.MarkAsSent(now);
        }

        await db.SaveChangesAsync(stoppingToken);

        foreach (var payload in pendingPayloads)
        {
            var unreadCount = await db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == payload.UserId && !n.IsRead)
                .CountAsync(stoppingToken);

            var hubPayload = new NotificationPayload
            {
                NotificationId = payload.Notification.NotificationId,
                Title = payload.Notification.Title,
                Message = payload.Notification.Message,
                CreatedAt = payload.Notification.CreatedAt,
                UnreadCount = unreadCount
            };

            await _hubContext.Clients.Group(NotificationHub.GetGroupName(payload.UserId))
                .SendAsync("NotificationReceived", hubPayload, stoppingToken);
        }
    }

    private sealed class PendingNotificationPayload
    {
        public string UserId { get; set; } = string.Empty;

        public Notification Notification { get; set; } = null!;
    }

    private static async Task<List<User>> GetRecipientsAsync(AppDbContext db, Appointment appointment, CancellationToken stoppingToken)
    {
        var recipients = new List<User>();

        if (appointment is GroupMeeting)
        {
            var participants = await db.AppointmentParticipants
                .Where(p => p.AppointmentId == appointment.AppointmentId && p.User != null)
                .Select(p => p.User!)
                .ToListAsync(stoppingToken);

            recipients.AddRange(participants);
        }

        if (appointment.Calendar?.User != null)
        {
            recipients.Add(appointment.Calendar.User);
        }

        return recipients
            .GroupBy(u => u.UserId)
            .Select(g => g.First())
            .ToList();
    }

    private static string BuildEmailBody(Appointment appointment, string message)
    {
        var location = string.IsNullOrWhiteSpace(appointment.Location) ? "(No location)" : appointment.Location;

        return $@"<h2>{appointment.Title}</h2>
<p><strong>Start:</strong> {appointment.StartTime:yyyy-MM-dd HH:mm}</p>
<p><strong>End:</strong> {appointment.EndTime:yyyy-MM-dd HH:mm}</p>
<p><strong>Location:</strong> {location}</p>
<p>{message}</p>";
    }
}
