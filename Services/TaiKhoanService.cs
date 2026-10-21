using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed record LoginOutcome(ClaimsPrincipal? Principal, int WaitSeconds = 0, bool Disabled = false);

public sealed class TaiKhoanService(AppDbContext db, UserManager<ApplicationUser> users)
{
    public const string Scheme = "EduConnect";
    private static readonly ApplicationUser Dummy = new();
    private static readonly string DummyHash = new PasswordHasher<ApplicationUser>().HashPassword(Dummy, "A dummy password for timing only");
    public static string NormalizePhone(string input)
    {
        var phone = new string(input.Where(char.IsAsciiDigit).ToArray());
        if (phone.StartsWith("84")) phone = "0" + phone[2..];
        return phone;
    }

    // SQL application locks serialize competing changes without holding a table-wide lock.
    private async Task LockAsync(string resource)
    {
        var name = "auth:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resource)));
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sp_getapplock @Resource={name}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000; IF @r < 0 THROW 51000, 'Authentication is busy.', 1;");
    }

    public async Task<IdentityResult> DangKyAsync(RegisterViewModel model)
    {
        var email = model.Email.Trim();
        var phone = NormalizePhone(model.Phone);
        if (phone.Length != 10 || phone[0] != '0')
            return IdentityResult.Failed(new IdentityError { Description = "Số điện thoại cần gồm 10 chữ số, bắt đầu bằng 0 hoặc +84." });
        await using var transaction = await db.Database.BeginTransactionAsync();
        await LockAsync("register");
        if (await users.FindByEmailAsync(email) != null || await db.Users.AnyAsync(x => x.PhoneNumber == phone))
            return IdentityResult.Failed(new IdentityError { Description = "Email hoặc số điện thoại đã được sử dụng. Vui lòng đăng nhập hoặc dùng thông tin khác." });
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, PhoneNumber = phone, HoTen = model.FullName.Trim(), TrangThai = 0 };
        var result = await users.CreateAsync(user, model.Password);
        if (!result.Succeeded) return result;
        var normalizedRole = model.Role.ToUpperInvariant();
        var role = await db.Roles.SingleOrDefaultAsync(x => x.NormalizedName == normalizedRole);
        if (role == null)
        {
            role = new IdentityRole<Guid>(model.Role) { Id = Guid.NewGuid(), NormalizedName = normalizedRole, ConcurrencyStamp = Guid.NewGuid().ToString() };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }
        result = await users.AddToRoleAsync(user, model.Role);
        if (!result.Succeeded) return result;
        await transaction.CommitAsync();
        return IdentityResult.Success;
    }

    public async Task<LoginOutcome> DangNhapAsync(LoginViewModel model, string ip)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Lock IP first everywhere, then canonical account: aliases cannot bypass account delays.
        await LockAsync("ip:" + ip);
        var identifier = model.Identifier.Trim();
        var user = identifier.Contains('@') ? await users.FindByEmailAsync(identifier)
            : await db.Users.SingleOrDefaultAsync(x => x.PhoneNumber == NormalizePhone(identifier));
        var key = user?.Id.ToString() ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identifier.ToUpperInvariant())));
        await LockAsync("account:" + key);
        var now = DateTimeOffset.UtcNow;
        var account = await GetAttempt(0, key, now);
        var address = await GetAttempt(1, ip, now);
        var waitUntil = new[] { account.ChoDenLuc, address.ChoDenLuc }.Max();
        if (waitUntil > now)
            return new(null, Math.Max(1, (int)Math.Ceiling((waitUntil.Value - now).TotalSeconds)));
        var valid = user != null ? await users.CheckPasswordAsync(user, model.Password)
            : new PasswordHasher<ApplicationUser>().VerifyHashedPassword(Dummy, DummyHash, model.Password) != PasswordVerificationResult.Failed;
        if (!valid || user == null)
        {
            RecordFailure(account, now);
            RecordFailure(address, now);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            var delay = new[] { account.ChoDenLuc, address.ChoDenLuc }.Max();
            return new(null, delay > now ? (int)Math.Ceiling((delay.Value - now).TotalSeconds) : 0);
        }
        if (user.TrangThai != 0) return new(null, Disabled: true);
        if (db.Entry(account).State != EntityState.Detached) db.LoginAttempts.Remove(account);
        var session = new UserSession { Id = Guid.NewGuid(), TaiKhoanId = user.Id, TaoLuc = now, HoatDongCuoiLuc = now };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        var roles = await users.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.HoTen),
            new("session_id", session.Id.ToString()), new("security_stamp", user.SecurityStamp ?? "")
        };
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
        await transaction.CommitAsync();
        return new(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme)));
    }

    private async Task<LoginAttempt> GetAttempt(int type, string key, DateTimeOffset now)
    {
        var attempt = await db.LoginAttempts.FindAsync(type, key);
        if (attempt == null) return new LoginAttempt { LoaiKhoa = type, GiaTriKhoa = key };
        if (attempt.LanSaiCuoiLuc <= now.AddMinutes(-15))
        {
            attempt.SoLanSai = 0;
            attempt.ChoDenLuc = null;
        }
        return attempt;
    }

    private void RecordFailure(LoginAttempt attempt, DateTimeOffset now)
    {
        attempt.SoLanSai++;
        attempt.LanSaiCuoiLuc = now;
        var seconds = attempt.SoLanSai switch { 5 => 30, 6 => 60, 7 => 120, >= 8 => 300, _ => 0 };
        attempt.ChoDenLuc = seconds == 0 ? null : now.AddSeconds(seconds);
        if (db.Entry(attempt).State == EntityState.Detached) db.LoginAttempts.Add(attempt);
    }

    public async Task ThuHoiPhienAsync(Guid userId, string reason)
    {
        await db.Sessions.Where(x => x.TaiKhoanId == userId && x.ThuHoiLuc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ThuHoiLuc, DateTimeOffset.UtcNow).SetProperty(x => x.LyDoThuHoi, reason));
    }
}
