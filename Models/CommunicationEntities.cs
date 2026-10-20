namespace KetNoiGiaSu.Models;

public enum ConnectionState { Requested, Proposed, Accepted, Rejected, Cancelled, Expired }
public sealed class ConnectionRequest
{
    public Guid Id { get; set; }
    public Guid GiaSuId { get; set; }
    public Guid BenHocId { get; set; }
    public Guid NguoiGuiId { get; set; }
    public Guid? NhuCauId { get; set; }
    public string? LoiNhan { get; set; }
    public ConnectionState TrangThai { get; set; }
    public DateTimeOffset GuiLuc { get; set; }
    public DateTimeOffset HetHanLuc { get; set; }
    public string? TenLopDuKien { get; set; }
    public int? MonHocId { get; set; }
    public string? MucTieu { get; set; }
    public int? SoBuoiTuan { get; set; }
    public int? ThoiLuongPhut { get; set; }
    public string? LichDuKien { get; set; }
    public DateTimeOffset? BatDauHocLuc { get; set; }
    public int? HinhThucHoc { get; set; }
    public string? DiaDiem { get; set; }
    public decimal? HocPhi { get; set; }
    public int? DonViHocPhi { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class Conversation
{
    public Guid Id { get; set; }
    public int LoaiHoiThoai { get; set; }
    public Guid? GiaSuId { get; set; }
    public Guid? BenHocId { get; set; }
    public Guid? LopHocId { get; set; }
    public Guid? HoiThoaiTruocKetNoiId { get; set; }
    public DateTimeOffset TaoLuc { get; set; }
}
public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid CuocHoiThoaiId { get; set; }
    public Guid NguoiGuiId { get; set; }
    public string NoiDung { get; set; } = "";
    public DateTimeOffset GuiLuc { get; set; }
    public Guid MaThaoTac { get; set; }
}
public sealed class ConnectedClass
{
    public Guid Id { get; set; }
    public Guid YeuCauKetNoiId { get; set; }
    public Guid GiaSuId { get; set; }
    public Guid BenHocId { get; set; }
    public string TenLop { get; set; } = "";
    public int MonHocId { get; set; }
    public string MucTieu { get; set; } = "";
    public int SoBuoiTuan { get; set; }
    public int ThoiLuongPhut { get; set; }
    public int HinhThucHoc { get; set; }
    public string? DiaDiem { get; set; }
    public decimal HocPhi { get; set; }
    public int DonViHocPhi { get; set; }
    public int TrangThai { get; set; }
    public DateTimeOffset TaoLuc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
