namespace KetNoiGiaSu.Models;

public sealed class TutorConditions
{
    public Guid HoSoGiaSuId { get; set; }
    public bool DayTrucTuyen { get; set; }
    public bool DayTrucTiep { get; set; }
    public string? KhuVuc { get; set; }
    public decimal? HocPhi { get; set; }
    public int? DonViHocPhi { get; set; }
    public bool DangNhanHocVien { get; set; }
    public DateTimeOffset CapNhatLuc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class TutorAvailability
{
    public Guid Id { get; set; }
    public Guid HoSoGiaSuId { get; set; }
    public int ThuTrongTuan { get; set; }
    public TimeOnly GioBatDau { get; set; }
    public TimeOnly GioKetThuc { get; set; }
}
public sealed class TutorProfileRevision
{
    public Guid Id { get; set; }
    public Guid HoSoGiaSuId { get; set; }
    public string TenHienThi { get; set; } = "";
    public string? AnhDaiDien { get; set; }
    public string TrinhDo { get; set; } = "";
    public string TruongDaoTao { get; set; } = "";
    public string ChuyenNganh { get; set; } = "";
    public string TinhTrangHocTap { get; set; } = "";
    public string ChuyenMon { get; set; } = "";
    public string KinhNghiem { get; set; } = "";
    public string MoTa { get; set; } = "";
    public string MonDayJson { get; set; } = "[]";
    public string MinhChungJson { get; set; } = "[]";
    public ProfileReviewState TrangThai { get; set; }
    public string? LyDoTuChoi { get; set; }
    public Guid? NguoiDuyetId { get; set; }
    public DateTimeOffset? DuyetLuc { get; set; }
    public DateTimeOffset TaoLuc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public enum ContactState { Pending, Accepted, Rejected, Withdrawn }
public sealed class ContactRequest
{
    public Guid Id { get; set; }
    public Guid GiaSuId { get; set; }
    public Guid BenHocId { get; set; }
    public Guid NguoiGuiId { get; set; }
    public string LoiNhan { get; set; } = "";
    public ContactState TrangThai { get; set; }
    public DateTimeOffset GuiLuc { get; set; }
    public DateTimeOffset? PhanHoiLuc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
