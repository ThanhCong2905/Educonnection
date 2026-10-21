using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public static class HoSoGiaSuRules
{
    public static IQueryable<TutorProfile> PublicProfiles(AppDbContext db) => db.TutorProfiles.AsNoTracking().Where(p =>
        p.TrangThaiDuyet == ProfileReviewState.Approved && p.TenHienThi != "" && p.AnhDaiDien != null && p.AnhDaiDien != "" &&
        p.TrinhDo != "" && p.TruongDaoTao != "" && p.ChuyenNganh != "" && p.TinhTrangHocTap != "" &&
        p.ChuyenMon != "" && p.KinhNghiem != "" && p.MoTa != "" &&
        db.Users.Any(u=>u.Id == p.GiaSuId && u.TrangThai == 0) &&
        db.TeachingSubjects.Any(s=>s.HoSoGiaSuId == p.Id && db.Subjects.Any(m=>m.Id==s.MonHocId && m.DangSuDung)) &&
        db.ProfileAttachments.Any(a=>a.HoSoGiaSuId == p.Id && a.NguoiTaiLenId == p.GiaSuId) &&
        db.TutorConditions.Any(c=>c.HoSoGiaSuId == p.Id && (c.DayTrucTuyen || c.DayTrucTiep) &&
            (!c.DayTrucTiep || (c.KhuVuc != null && c.KhuVuc != "")) &&
            (!c.DangNhanHocVien || db.TutorAvailability.Any(s=>s.HoSoGiaSuId == p.Id))));

    internal static void CheckVersion(byte[] version, string? supplied)
    {
        byte[] value;
        try { value = Convert.FromBase64String(supplied ?? ""); }
        catch (FormatException) { throw new InvalidOperationException("Phiên bản dữ liệu không hợp lệ. Vui lòng tải lại trang."); }
        if (!version.SequenceEqual(value)) throw new InvalidOperationException("Thông tin đã thay đổi. Vui lòng tải lại trang trước khi lưu.");
    }
}
