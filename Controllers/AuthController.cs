using ChronoPlan.Services;
using ChronoPlan.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChronoPlan.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _authService.LoginAsync(model.Email, model.Password);

        if (user == null)
        {
            ModelState.AddModelError("", "Email or password is incorrect, or the account is not verified.");
            return View(model);
        }

        HttpContext.Session.SetString("UserId", user.UserId);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("UserEmail", user.Email);

        return RedirectToAction("Index", "Calendar");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        return RedirectToAction(nameof(VerifyOtp), new { email = model.Email });
    }

    [HttpGet]
    public IActionResult VerifyOtp(string email)
    {
        return View(new VerifyOtpViewModel
        {
            Email = email
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.VerifyRegistrationOtpAsync(model.Email, model.Otp);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["Success"] = "Verification successful. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _authService.SendResetOtpAsync(model.Email);

        return RedirectToAction(nameof(VerifyResetOtp), new { email = model.Email });
    }

    [HttpGet]
    public IActionResult VerifyResetOtp(string email)
    {
        return View(new VerifyOtpViewModel
        {
            Email = email
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyResetOtp(VerifyOtpViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.VerifyResetOtpAsync(model.Email, model.Otp);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        HttpContext.Session.SetString("ResetEmail", model.Email);
        HttpContext.Session.SetString("ResetVerified", "true");

        return RedirectToAction(nameof(ResetPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        var resetVerified = HttpContext.Session.GetString("ResetVerified");
        if (resetVerified != "true")
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var resetVerified = HttpContext.Session.GetString("ResetVerified");
        var email = HttpContext.Session.GetString("ResetEmail");

        if (resetVerified != "true" || string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ResetPasswordAsync(email, model.NewPassword);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        HttpContext.Session.Remove("ResetVerified");
        HttpContext.Session.Remove("ResetEmail");

        TempData["Success"] = "Password changed successfully. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
