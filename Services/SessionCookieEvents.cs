using System.Security.Claims;
using KetNoiGiaSu.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class SessionCookieEvents(AppDbContext db) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var valid = Guid.TryParse(context.Principal?.FindFirstValue("session_id"), out var sessionId)
            && Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out _);
        var now = DateTimeOffset.UtcNow;
        if (valid)
        {
            var userId = Guid.Parse(context.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var stamp = context.Principal!.FindFirstValue("security_stamp");
            var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId && x.TaiKhoanId == userId);
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId);
            valid = session != null && session.ThuHoiLuc == null && session.HoatDongCuoiLuc > now.AddMinutes(-30)
                && user != null && user.TrangThai == 0 && user.SecurityStamp == stamp;
            if (valid)
            {
                var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == userId select r.Name!).ToArrayAsync();
                valid = roles.Order().SequenceEqual(context.Principal!.FindAll(ClaimTypes.Role).Select(x => x.Value).Order());
            }
            if (valid && context.Request.Headers["X-Background-Request"] != "true")
            {
                var changed = await db.Sessions.Where(x => x.Id == sessionId && x.ThuHoiLuc == null && x.HoatDongCuoiLuc > now.AddMinutes(-30))
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.HoatDongCuoiLuc, now));
                valid = changed == 1;
            }
        }
        if (!valid)
        {
            if (Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var rejectedUserId))
                await db.Sessions.Where(x => x.Id == sessionId && x.TaiKhoanId == rejectedUserId && x.ThuHoiLuc == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ThuHoiLuc, now).SetProperty(x => x.LyDoThuHoi, "Phiên hết hạn hoặc quyền đã thay đổi"));
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(AccountService.Scheme);
        }
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) => Redirect(context, 401);
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) => Redirect(context, 403);
    private static Task Redirect(RedirectContext<CookieAuthenticationOptions> context, int status)
    {
        if (context.Request.Path.StartsWithSegments("/api") || context.Request.Headers.Accept.ToString().Contains("application/json") || context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            context.Response.StatusCode = status;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
