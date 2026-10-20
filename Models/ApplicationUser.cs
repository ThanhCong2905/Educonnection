using Microsoft.AspNetCore.Identity;

namespace KetNoiGiaSu.Models;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string HoTen { get; set; } = "";
    public string? AnhDaiDien { get; set; }
    public int TrangThai { get; set; }
}

public sealed class UserSession
{
    public Guid Id { get; set; }
    public Guid TaiKhoanId { get; set; }
    public DateTimeOffset TaoLuc { get; set; }
    public DateTimeOffset HoatDongCuoiLuc { get; set; }
    public DateTimeOffset? ThuHoiLuc { get; set; }
    public string? LyDoThuHoi { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class LoginAttempt
{
    public int LoaiKhoa { get; set; }
    public string GiaTriKhoa { get; set; } = "";
    public int SoLanSai { get; set; }
    public DateTimeOffset LanSaiCuoiLuc { get; set; }
    public DateTimeOffset? ChoDenLuc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
