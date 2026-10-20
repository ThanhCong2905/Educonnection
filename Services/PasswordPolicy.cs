using KetNoiGiaSu.Models;
using Microsoft.AspNetCore.Identity;

namespace KetNoiGiaSu.Services;

public sealed class PasswordPolicy(IWebHostEnvironment environment) : IPasswordValidator<ApplicationUser>
{
    private readonly HashSet<string> blocked = File.ReadAllLines(Path.Combine(environment.ContentRootPath, "Data", "blocked-passwords.txt"))
        .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith('#')).Select(x => x.ToUpperInvariant()).ToHashSet();

    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        var errors = new List<IdentityError>();
        if (password is null || password.Length is < 15 or > 64)
            errors.Add(new() { Description = "Mật khẩu cần từ 15 đến 64 ký tự; có thể dùng khoảng trắng." });
        else if (blocked.Contains(password.ToUpperInvariant()))
            errors.Add(new() { Description = "Mật khẩu quá phổ biến hoặc đã bị lộ. Vui lòng chọn mật khẩu khác." });
        return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
    }
}
