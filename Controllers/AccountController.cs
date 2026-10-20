using System.Security.Claims;
using System.Text;
using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.Services;
using KetNoiGiaSu.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Controllers;

[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountController(AccountService accounts, UserManager<ApplicationUser> users,
    AppDbContext db, AccountEmailSender emailSender, IConfiguration configuration, ILogger<AccountController> logger) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true ? RedirectToAction(nameof(Overview)) : View(new RegisterViewModel());

    [HttpPost, AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Overview));
        if (!ModelState.IsValid) return View(model);
        var result = await accounts.RegisterAsync(model);
        if (!result.Succeeded) { AddErrors(result); return View(model); }
        TempData["Success"] = "Tạo tài khoản thành công. Bạn có thể đăng nhập ngay.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Overview));
        return View(new LoginViewModel { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await accounts.LoginAsync(model, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        if (result.Principal == null)
        {
            if (result.WaitSeconds > 0)
            {
                Response.StatusCode = StatusCodes.Status429TooManyRequests;
                Response.Headers.RetryAfter = result.WaitSeconds.ToString();
                ViewData["RetrySeconds"] = result.WaitSeconds;
                ModelState.AddModelError("", $"Bạn đã đăng nhập sai nhiều lần. Vui lòng thử lại sau {result.WaitSeconds} giây.");
            }
            else ModelState.AddModelError("", result.Disabled ? "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên." : "Email / số điện thoại hoặc mật khẩu chưa đúng.");
            return View(model);
        }
        // Remembering the cookie never bypasses server-side 30-minute inactivity expiry.
        await HttpContext.SignInAsync(AccountService.Scheme, result.Principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe, AllowRefresh = true });
        if (Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl!);
        return result.Principal.IsInRole("HocSinh") || result.Principal.IsInRole("PhuHuynh") || result.Principal.IsInRole("GiaSu")
            ? RedirectToAction("Index", "Workspace") : RedirectToAction(nameof(Overview));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        if (Guid.TryParse(User.FindFirstValue("session_id"), out var sessionId))
            await db.Sessions.Where(x => x.Id == sessionId && x.ThuHoiLuc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ThuHoiLuc, DateTimeOffset.UtcNow).SetProperty(x => x.LyDoThuHoi, "Đăng xuất"));
        await HttpContext.SignOutAsync(AccountService.Scheme);
        TempData["Success"] = "Bạn đã đăng xuất an toàn.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user != null && user.TrangThai == 0)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var baseUrl = configuration["Account:PublicBaseUrl"] ?? throw new InvalidOperationException("Account:PublicBaseUrl is missing.");
            var path = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, code = encoded });
            try { await emailSender.SendResetAsync(user.Email!, baseUrl.TrimEnd('/') + path); }
            catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException or IOException)
            {
                // Do not log the exception message: it can contain addresses or a reset URL.
                logger.LogError("Password reset email delivery failed ({Type}). TraceId {TraceId}", ex.GetType().Name, HttpContext.TraceIdentifier);
            }
        }
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? code)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code)) return View("ResetLinkInvalid");
        return View(new ResetPasswordViewModel { Email = email, Code = code });
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code)); }
        catch (FormatException) { return View("ResetLinkInvalid"); }
        if (user == null || user.TrangThai != 0) return View("ResetLinkInvalid");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var result = await users.ResetPasswordAsync(user, token, model.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(x => x.Code == "InvalidToken")) return View("ResetLinkInvalid");
            AddErrors(result);
            return View(model);
        }
        await accounts.RevokeSessionsAsync(user.Id, "Đặt lại mật khẩu");
        await transaction.CommitAsync();
        await HttpContext.SignOutAsync(AccountService.Scheme);
        TempData["Success"] = "Đã đặt lại mật khẩu. Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, Authorize, EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.Password);
        if (!result.Succeeded) { AddErrors(result); return View(model); }
        await accounts.RevokeSessionsAsync(user.Id, "Đổi mật khẩu");
        await transaction.CommitAsync();
        await HttpContext.SignOutAsync(AccountService.Scheme);
        TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> Overview() => View(await users.GetUserAsync(User));

    [HttpGet]
    public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Code switch
            {
                "PasswordMismatch" => "Mật khẩu hiện tại không chính xác.",
                "DuplicateEmail" or "DuplicateUserName" => "Email đã được sử dụng.",
                "ConcurrencyFailure" => "Thông tin vừa thay đổi. Vui lòng tải lại trang và thử lại.",
                _ => error.Description
            });
    }
}
