namespace KetNoiGiaSu.Models;

public enum NeedState { Draft, Open, Closed }
public enum ProfileReviewState { Pending, Approved, Rejected }

public sealed class Subject
{
    public int Id { get; set; }
    public string TenMon { get; set; } = "";
    public bool DangSuDung { get; set; }
}

public sealed class TutorProfile
{
    public Guid Id { get; set; }
    public Guid GiaSuId { get; set; }
    public string TrinhDo { get; set; } = "";
    public string ChuyenMon { get; set; } = "";
    public string KinhNghiem { get; set; } = "";
    public string MoTa { get; set; } = "";
    public string KhuVuc { get; set; } = "";
    public decimal HocPhiCongKhai { get; set; }
    public int DonViHocPhi { get; set; }
    public ProfileReviewState TrangThaiDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }
    public Guid? NguoiDuyetId { get; set; }
    public DateTimeOffset? DuyetLuc { get; set; }
    public DateTimeOffset TaoLuc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TeachingSubject
{
    public Guid HoSoGiaSuId { get; set; }
    public int MonHocId { get; set; }
    public string PhamViDay { get; set; } = "";
}

public sealed class LearningNeed
{
    public Guid Id { get; set; }
    public Guid ChuSoHuuId { get; set; }
    public int LoaiNhuCau { get; set; }
    public int MonHocId { get; set; }
    public string? KhoiLop { get; set; }
    public string? HocLuc { get; set; }
    public string? MucTieu { get; set; }
    public int? SoBuoiTuan { get; set; }
    public int HinhThucHoc { get; set; }
    public string? KhuVuc { get; set; }
    public string? DiaChiChiTiet { get; set; }
    public decimal? HocPhiDuKien { get; set; }
    public int? DonViHocPhi { get; set; }
    public string? TenNguoiHoc { get; set; }
    public int? TuoiNguoiHoc { get; set; }
    public NeedState TrangThai { get; set; }
    public DateTimeOffset TaoLuc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AvailabilitySlot
{
    public Guid Id { get; set; }
    public Guid NhuCauId { get; set; }
    public int ThuTrongTuan { get; set; }
    public TimeOnly GioBatDau { get; set; }
    public TimeOnly GioKetThuc { get; set; }
}
