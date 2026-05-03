using System.Security.Cryptography;
using ChronoPlan.Data;
using ChronoPlan.Domain.Entities;
using ChronoPlan.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ChronoPlan.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext db,
        PasswordHasher passwordHasher,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterViewModel model)
    {
        var email = model.Email.Trim().ToLowerInvariant();

        if (!email.EndsWith("@gmail.com"))
        {
            return (false, "Please register with a Gmail account.");
        }

        var existingUser = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existingUser != null)
        {
            if (existingUser.IsEmailVerified)
            {
                return (false, "This email is already in use.");
            }

            var resendOtp = GenerateOtp();
            var resendOtpMinutes = _configuration.GetValue<int>("AppSettings:OtpMinutes", 10);

            existingUser.Name = model.Name.Trim();
            existingUser.PhoneNumber = model.PhoneNumber;
            existingUser.PasswordHash = _passwordHasher.Hash(model.Password);
            existingUser.RegisterOtpHash = _passwordHasher.Hash(resendOtp);
            existingUser.RegisterOtpExpiresAt = DateTime.UtcNow.AddMinutes(resendOtpMinutes);

            await _db.SaveChangesAsync();

            try
            {
                await _emailSender.SendAsync(
                    email,
                    "ChronoPlan - Verify your account",
                    BuildOtpEmail("Verify your ChronoPlan account", resendOtp)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend registration OTP email to {Email}", email);
                return (false, "Failed to send OTP via Gmail. Please check SMTP settings and try again.");
            }

            return (true, "Verification OTP has been resent. Please check Gmail.");
        }

        var otp = GenerateOtp();
        var otpMinutes = _configuration.GetValue<int>("AppSettings:OtpMinutes", 10);

        var user = new User
        {
            Name = model.Name.Trim(),
            Email = email,
            PhoneNumber = model.PhoneNumber,
            PasswordHash = _passwordHasher.Hash(model.Password),
            IsEmailVerified = false,
            RegisterOtpHash = _passwordHasher.Hash(otp),
            RegisterOtpExpiresAt = DateTime.UtcNow.AddMinutes(otpMinutes)
        };

        var calendar = new Calendar
        {
            UserId = user.UserId
        };

        _db.Users.Add(user);
        _db.Calendars.Add(calendar);

        await _db.SaveChangesAsync();

        try
        {
            await _emailSender.SendAsync(
                email,
                "ChronoPlan - Verify your account",
                BuildOtpEmail("Verify your ChronoPlan account", otp)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration OTP email to {Email}", email);

            _db.Calendars.Remove(calendar);
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return (false, "Không gửi được OTP qua Gmail. Vui lòng kiểm tra cấu hình SMTP và thử lại.");
        }

        return (true, "Registration successful. Please check Gmail for the OTP.");
    }

    public async Task<(bool Success, string Message)> VerifyRegistrationOtpAsync(string email, string otp)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            return (false, "Account not found.");
        }

        if (user.IsEmailVerified)
        {
            return (true, "Account has been verified.");
        }

        if (user.RegisterOtpHash == null || user.RegisterOtpExpiresAt == null)
        {
            return (false, "OTP does not exist.");
        }

        if (DateTime.UtcNow > user.RegisterOtpExpiresAt.Value)
        {
            return (false, "OTP has expired.");
        }

        if (!_passwordHasher.Verify(otp, user.RegisterOtpHash))
        {
            return (false, "OTP không đúng.");
        }

        user.IsEmailVerified = true;
        user.RegisterOtpHash = null;
        user.RegisterOtpExpiresAt = null;

        await _db.SaveChangesAsync();

        return (true, "Xác thực thành công. Bạn có thể đăng nhập.");
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
        {
            return null;
        }

        if (!user.IsEmailVerified)
        {
            return null;
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public async Task SendResetOtpAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            return;
        }

        var otp = GenerateOtp();
        var otpMinutes = _configuration.GetValue<int>("AppSettings:OtpMinutes", 10);

        user.ResetOtpHash = _passwordHasher.Hash(otp);
        user.ResetOtpExpiresAt = DateTime.UtcNow.AddMinutes(otpMinutes);

        await _db.SaveChangesAsync();

        await _emailSender.SendAsync(
            email,
            "ChronoPlan - Reset password OTP",
            BuildOtpEmail("Reset your ChronoPlan password", otp)
        );
    }

    public async Task<(bool Success, string Message)> VerifyResetOtpAsync(string email, string otp)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            return (false, "Không tìm thấy tài khoản.");
        }

        if (user.ResetOtpHash == null || user.ResetOtpExpiresAt == null)
        {
            return (false, "OTP không tồn tại.");
        }

        if (DateTime.UtcNow > user.ResetOtpExpiresAt.Value)
        {
            return (false, "OTP đã hết hạn.");
        }

        if (!_passwordHasher.Verify(otp, user.ResetOtpHash))
        {
            return (false, "OTP không đúng.");
        }

        return (true, "OTP hợp lệ.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string newPassword)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            return (false, "Không tìm thấy tài khoản.");
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.ResetOtpHash = null;
        user.ResetOtpExpiresAt = null;

        await _db.SaveChangesAsync();

        return (true, "Đổi mật khẩu thành công.");
    }

    private static string GenerateOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    private static string BuildOtpEmail(string title, string otp)
    {
        return $"""
        <div style="font-family:Arial,sans-serif">
            <h2>{title}</h2>
            <p>Your OTP code is:</p>
            <h1 style="letter-spacing:6px">{otp}</h1>
            <p>This code will expire soon.</p>
        </div>
        """;
    }
}
