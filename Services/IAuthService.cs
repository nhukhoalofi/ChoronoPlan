using ChronoPlan.Domain.Entities;
using ChronoPlan.ViewModels;

namespace ChronoPlan.Services;

public interface IAuthService
{
    Task<(bool Success, string Message)> RegisterAsync(RegisterViewModel model);

    Task<(bool Success, string Message)> VerifyRegistrationOtpAsync(string email, string otp);

    Task<User?> LoginAsync(string email, string password);

    Task SendResetOtpAsync(string email);

    Task<(bool Success, string Message)> VerifyResetOtpAsync(string email, string otp);

    Task<(bool Success, string Message)> ResetPasswordAsync(string email, string newPassword);
}
