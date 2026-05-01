using ChronoPlan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChronoPlan.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Calendar> Calendars => Set<Calendar>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<GroupMeeting> GroupMeetings => Set<GroupMeeting>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AppointmentParticipant> AppointmentParticipants => Set<AppointmentParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.UserId);

            entity.Property(x => x.UserId).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.PasswordHash).IsRequired();

            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Calendar>(entity =>
        {
            entity.ToTable("Calendars");
            entity.HasKey(x => x.CalendarId);

            entity.Property(x => x.CalendarId).HasMaxLength(50);
            entity.Property(x => x.UserId).HasMaxLength(50).IsRequired();

            entity.HasIndex(x => x.UserId).IsUnique();

            entity.HasOne(x => x.User)
                .WithOne(x => x.Calendar)
                .HasForeignKey<Calendar>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(x => x.AppointmentId);

            entity.Property(x => x.AppointmentId).HasMaxLength(50);
            entity.Property(x => x.CalendarId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(200);
            entity.Property(x => x.StartTime).IsRequired();
            entity.Property(x => x.EndTime).IsRequired();

            entity.HasOne(x => x.Calendar)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.CalendarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupMeeting>(entity =>
        {
            entity.ToTable("GroupMeetings");
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.ToTable("Reminders");
            entity.HasKey(x => x.ReminderId);

            entity.Property(x => x.ReminderId).HasMaxLength(50);
            entity.Property(x => x.AppointmentId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(255);

            entity.Property(x => x.IsCanceled).IsRequired();
            entity.Property(x => x.IsSent).IsRequired();
            entity.Property(x => x.SentAt);

            entity.HasOne(x => x.Appointment)
                .WithMany(x => x.Reminders)
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.NotificationId);

            entity.Property(x => x.NotificationId).HasMaxLength(50);
            entity.Property(x => x.UserId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AppointmentId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ReminderId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(500).IsRequired();
            entity.Property(x => x.IsRead).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Reminder)
                .WithMany()
                .HasForeignKey(x => x.ReminderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppointmentParticipant>(entity =>
        {
            entity.ToTable("AppointmentParticipants");
            entity.HasKey(x => new { x.AppointmentId, x.UserId });

            entity.Property(x => x.AppointmentId).HasMaxLength(50);
            entity.Property(x => x.UserId).HasMaxLength(50);

            entity.HasOne(x => x.GroupMeeting)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.AppointmentParticipants)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
