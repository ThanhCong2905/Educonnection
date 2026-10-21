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
public sealed class TaiKhoanController(TaiKhoanService accounts, UserManager<ApplicationUser> users,
    AppDbContext db, AccountEmailSender emailSender, IConfiguration configuration, ILogger<TaiKhoanController> logger) : Controller
{
    [HttpGet, AllowAnonymous]
    [Route("/TaiKhoan/DangKy")]
    [Route("/Account/Register")]
    public IActionResult DangKy() => User.Identity?.IsAuthenticated == true ? RedirectToAction(nameof(TongQuan)) : View(new RegisterViewModel());

    [HttpPost, AllowAnonymous, EnableRateLimiting("auth")]
    [Route("/TaiKhoan/DangKy")]
    [Route("/Account/Register")]
    public async Task<IActionResult> DangKy(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(TongQuan));
        if (!ModelState.IsValid) return View(model);
        var result = await accounts.DangKyAsync(model);
        if (!result.Succeeded) { AddErrors(result); return View(model); }
        TempData["Success"] = "Tạo tài khoản thành công. Bạn có thể đăng nhập ngay.";
        return RedirectToAction(nameof(DangNhap));
    }

    [HttpGet, AllowAnonymous]
    [Route("/TaiKhoan/DangNhap")]
    [Route("/Account/Login")]
    public IActionResult DangNhap(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(TongQuan));
        return View(new LoginViewModel { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting("auth")]
    [Route("/TaiKhoan/DangNhap")]
    [Route("/Account/Login")]
    public async Task<IActionResult> DangNhap(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await accounts.DangNhapAsync(model, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
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
        await HttpContext.SignInAsync(TaiKhoanService.Scheme, result.Principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe, AllowRefresh = true });
        if (Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl!);
        return result.Principal.IsInRole("HocSinh") || result.Principal.IsInRole("PhuHuynh") || result.Principal.IsInRole("GiaSu")
            ? RedirectToAction("Index", "Workspace") : RedirectToAction(nameof(TongQuan));
    }

    [HttpPost, Authorize]
    [Route("/TaiKhoan/DangXuat")]
    [Route("/Account/Logout")]
    public async Task<IActionResult> DangXuat()
    {
        if (Guid.TryParse(User.FindFirstValue("session_id"), out var sessionId))
            await db.Sessions.Where(x => x.Id == sessionId && x.ThuHoiLuc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ThuHoiLuc, DateTimeOffset.UtcNow).SetProperty(x => x.LyDoThuHoi, "Đăng xuất"));
        await HttpContext.SignOutAsync(TaiKhoanService.Scheme);
        TempData["Success"] = "Bạn đã đăng xuất an toàn.";
        return RedirectToAction(nameof(DangNhap));
    }

    [HttpGet, AllowAnonymous]
    [Route("/TaiKhoan/QuenMatKhau")]
    [Route("/Account/ForgotPassword")]
    public IActionResult QuenMatKhau() => View(new ForgotPasswordViewModel());

    [HttpPost, AllowAnonymous, EnableRateLimiting("auth")]
    [Route("/TaiKhoan/QuenMatKhau")]
    [Route("/Account/ForgotPassword")]
    public async Task<IActionResult> QuenMatKhau(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user != null && user.TrangThai == 0)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var baseUrl = configuration["Account:PublicBaseUrl"] ?? throw new InvalidOperationException("Account:PublicBaseUrl is missing.");
            var path = Url.Action(nameof(DatLaiMatKhau), "TaiKhoan", new { email = user.Email, code = encoded });
            try { await emailSender.SendResetAsync(user.Email!, baseUrl.TrimEnd('/') + path); }
            catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException or IOException)
            {
                // Do not log the exception message: it can contain addresses or a reset URL.
                logger.LogError("Password reset email delivery failed ({Type}). TraceId {TraceId}", ex.GetType().Name, HttpContext.TraceIdentifier);
            }
        }
        return RedirectToAction(nameof(XacNhanQuenMatKhau));
    }

    [HttpGet, AllowAnonymous]
    [Route("/TaiKhoan/XacNhanQuenMatKhau")]
    [Route("/Account/ForgotPasswordConfirmation")]
    public IActionResult XacNhanQuenMatKhau() => View();

    [HttpGet, AllowAnonymous]
    [Route("/TaiKhoan/DatLaiMatKhau")]
    [Route("/Account/ResetPassword")]
    public IActionResult DatLaiMatKhau(string? email, string? code)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code)) return View("ResetLinkInvalid");
        return View(new ResetPasswordViewModel { Email = email, Code = code });
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting("auth")]
    [Route("/TaiKhoan/DatLaiMatKhau")]
    [Route("/Account/ResetPassword")]
    public async Task<IActionResult> DatLaiMatKhau(ResetPasswordViewModel model)
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
        await accounts.ThuHoiPhienAsync(user.Id, "Đặt lại mật khẩu");
        await transaction.CommitAsync();
        await HttpContext.SignOutAsync(TaiKhoanService.Scheme);
        TempData["Success"] = "Đã đặt lại mật khẩu. Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(DangNhap));
    }

    [HttpGet, Authorize]
    [Route("/TaiKhoan/DoiMatKhau")]
    [Route("/Account/ChangePassword")]
    public IActionResult DoiMatKhau() => View(new ChangePasswordViewModel());

    [HttpPost, Authorize, EnableRateLimiting("auth")]
    [Route("/TaiKhoan/DoiMatKhau")]
    [Route("/Account/ChangePassword")]
    public async Task<IActionResult> DoiMatKhau(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.Password);
        if (!result.Succeeded) { AddErrors(result); return View(model); }
        await accounts.ThuHoiPhienAsync(user.Id, "Đổi mật khẩu");
        await transaction.CommitAsync();
        await HttpContext.SignOutAsync(TaiKhoanService.Scheme);
        TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
        return RedirectToAction(nameof(DangNhap));
    }

    [HttpGet, Authorize]
    [Route("/TaiKhoan/TongQuan")]
    [Route("/Account/Overview")]
    public async Task<IActionResult> TongQuan() => View(await users.GetUserAsync(User));

    [HttpGet]
    [Route("/TaiKhoan/TuChoiTruyCap")]
    [Route("/Account/AccessDenied")]
    public IActionResult TuChoiTruyCap() { Response.StatusCode = 403; return View(); }

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
