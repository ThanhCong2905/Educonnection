namespace KetNoiGiaSu.Models;

public sealed class PersonalProfile
{
    public Guid TaiKhoanId { get; set; }
    public string ThanhPho { get; set; } = "";
    public string QuanHuyen { get; set; } = "";
    public string? DiaChiChiTiet { get; set; }
    public string PublicRegion => string.Join(", ", new[] { QuanHuyen, ThanhPho }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public sealed class ProfileAttachment
{
    public Guid Id { get; set; }
    public Guid NguoiTaiLenId { get; set; }
    public string TenGoc { get; set; } = "";
    public string TenLuuTru { get; set; } = "";
    public string LoaiNoiDung { get; set; } = "";
    public long KichThuoc { get; set; }
    public string Checksum { get; set; } = "";
    public int TrangThaiQuet { get; set; }
    public DateTimeOffset TaoLuc { get; set; }
    public Guid? HoSoGiaSuId { get; set; }
}
