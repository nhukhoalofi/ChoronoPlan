namespace ChronoPlan.Domain.Entities;

public class User
{
    public string UserId { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public string? RegisterOtpHash { get; set; }

    public DateTime? RegisterOtpExpiresAt { get; set; }

    public string? ResetOtpHash { get; set; }

    public DateTime? ResetOtpExpiresAt { get; set; }

    public Calendar? Calendar { get; set; }

    public ICollection<AppointmentParticipant> AppointmentParticipants { get; set; }
        = new List<AppointmentParticipant>();
}
