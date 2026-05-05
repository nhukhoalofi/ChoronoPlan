namespace ChronoPlan.Domain.Entities;

public class User
{
    public string UserId { get; protected set; } = Guid.NewGuid().ToString("N");

    public string Name { get; protected set; } = string.Empty;

    public string Email { get; protected set; } = string.Empty;

    public string? PhoneNumber { get; protected set; }

    public string PasswordHash { get; protected set; } = string.Empty;

    public bool IsEmailVerified { get; protected set; }

    public string? RegisterOtpHash { get; protected set; }

    public DateTime? RegisterOtpExpiresAt { get; protected set; }

    public string? ResetOtpHash { get; protected set; }

    public DateTime? ResetOtpExpiresAt { get; protected set; }

    public Calendar? Calendar { get; set; }

    public ICollection<AppointmentParticipant> AppointmentParticipants { get; private set; }
        = new List<AppointmentParticipant>();

    public ICollection<Notification> Notifications { get; private set; }
        = new List<Notification>();

    public static User Create(
        string name,
        string email,
        string? phoneNumber,
        string passwordHash,
        string registerOtpHash,
        DateTime registerOtpExpiresAt)
    {
        var user = new User();
        user.UpdateRegistrationInfo(name, phoneNumber, passwordHash, registerOtpHash, registerOtpExpiresAt);
        user.Email = email;
        user.IsEmailVerified = false;

        return user;
    }

    public void UpdateRegistrationInfo(
        string name,
        string? phoneNumber,
        string passwordHash,
        string registerOtpHash,
        DateTime registerOtpExpiresAt)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        PasswordHash = passwordHash;
        RegisterOtpHash = registerOtpHash;
        RegisterOtpExpiresAt = registerOtpExpiresAt;
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        RegisterOtpHash = null;
        RegisterOtpExpiresAt = null;
    }

    public void SetResetOtp(string resetOtpHash, DateTime resetOtpExpiresAt)
    {
        ResetOtpHash = resetOtpHash;
        ResetOtpExpiresAt = resetOtpExpiresAt;
    }

    public void ResetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        ResetOtpHash = null;
        ResetOtpExpiresAt = null;
    }
}
