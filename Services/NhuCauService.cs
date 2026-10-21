using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class NhuCauService(AppDbContext db, IConfiguration? configuration = null, IWebHostEnvironment? environment = null)
{
    public Task TaoNhuCauHocAsync(Guid owner, WorkspaceViewModel workspace, NeedForm form)
    {
        if (workspace.IsTutor || form.Id.HasValue) throw new InvalidOperationException("Dữ liệu tạo nhu cầu học không hợp lệ.");
        return CapNhatAsync(owner, workspace, form);
    }

    public Task TaoNhuCauNhanLopAsync(Guid owner, WorkspaceViewModel workspace, NeedForm form)
    {
        throw new InvalidOperationException("Nhu cầu dạy đã được thay bằng hồ sơ gia sư và điều kiện nhận lớp.");
    }

    private bool BypassApproval => environment?.IsDevelopment() == true && configuration?.GetValue<bool>("Workspace:BypassTutorApproval") == true;
    private async Task EnsureActiveAsync(Guid owner)
    {
        var accounts = await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.AspNetUsers WITH (UPDLOCK,HOLDLOCK) WHERE Id={owner}").ToListAsync();
        if (accounts.SingleOrDefault()?.TrangThai != 0) throw new InvalidOperationException("Tài khoản không còn hoạt động. Vui lòng đăng nhập lại.");
    }

    internal IQueryable<NeedCard> Catalog()
    {
        var query = from n in db.Needs.AsNoTracking()
                    join s in db.Subjects on n.MonHocId equals s.Id
                    join u in db.Users on n.ChuSoHuuId equals u.Id
                    where u.TrangThai == 0 && s.DangSuDung
                    select new { n, s, u };
        return query.Where(x => x.n.LoaiNhuCau == 0).Select(x => new NeedCard { Id = x.n.Id, OwnerId = x.u.Id,
            OwnerName = x.u.HoTen, Subject = x.s.TenMon, Grade = x.n.KhoiLop, Goal = x.n.MucTieu,
            Region = x.n.KhuVuc, Format = x.n.HinhThucHoc, Fee = x.n.HocPhiDuKien, FeeUnit = x.n.DonViHocPhi,
            WeeklySessions = x.n.SoBuoiTuan, Created = x.n.TaoLuc, State = x.n.TrangThai });
    }

    public async Task<List<NeedCard>> LayDanhSachAsync(Guid userId)
    {
        return await (from n in db.Needs.AsNoTracking() join s in db.Subjects on n.MonHocId equals s.Id
                      where n.ChuSoHuuId == userId && n.LoaiNhuCau == 0 orderby n.TaoLuc descending
                      select new NeedCard { Id = n.Id, OwnerId = userId, Subject = s.TenMon, Grade = n.KhoiLop,
                          Goal = n.MucTieu, Format = n.HinhThucHoc, Region = n.KhuVuc, Fee = n.HocPhiDuKien,
                          FeeUnit = n.DonViHocPhi, WeeklySessions = n.SoBuoiTuan, State = n.TrangThai,
                          IsTutor = n.LoaiNhuCau == 1, Created = n.TaoLuc }).ToListAsync();
    }

    public async Task<NeedCard?> LayChiTietAsync(Guid id)
    {
        var result = await Catalog().SingleOrDefaultAsync(x => x.Id == id && x.State == NeedState.Open);
        if (result != null) result.Slots = await db.Availability.AsNoTracking().Where(x => x.NhuCauId == id).OrderBy(x => x.ThuTrongTuan).ToListAsync();
        return result;
    }

    public Task<LearningNeed?> LayDeSuaAsync(Guid id, Guid owner) => db.Needs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ChuSoHuuId == owner);

    public Task<List<AvailabilitySlot>> LayKhungGioAsync(Guid id) => db.Availability.AsNoTracking().Where(x => x.NhuCauId == id).ToListAsync();

    public async Task CapNhatAsync(Guid owner, WorkspaceViewModel workspace, NeedForm form)
    {
        if(workspace.IsTutor || await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==owner&&r.Name=="GiaSu" select ur).AnyAsync())
            throw new InvalidOperationException("Gia sư cập nhật hồ sơ và điều kiện nhận lớp, không đăng nhu cầu dạy.");
        if (!workspace.CanPublish) throw new InvalidOperationException("Hồ sơ gia sư phải được duyệt trước khi tạo hoặc sửa nhu cầu nhận lớp.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        await EnsureActiveAsync(owner);
        // Serialize writes with profile approval changes, then recheck current approval.
        if (!await db.PersonalProfiles.AnyAsync(x => x.TaiKhoanId == owner && x.ThanhPho.Trim() != "" && x.DiaChiChiTiet != null && x.DiaChiChiTiet.Trim() != ""))
            throw new InvalidOperationException("Vui lòng hoàn thiện tỉnh/thành phố và địa chỉ chi tiết trong hồ sơ cá nhân trước khi tạo nhu cầu.");
        if (workspace.IsTutor && !await db.ProfileAttachments.AnyAsync(x => x.NguoiTaiLenId == owner && x.HoSoGiaSuId != null))
            throw new InvalidOperationException("Gia sư phải đính kèm ít nhất một bằng cấp hoặc chứng chỉ trong hồ sơ cá nhân.");
        if (workspace.IsTutor && !BypassApproval)
        {
            var profiles = await db.TutorProfiles.FromSqlInterpolated($"SELECT * FROM dbo.HoSoGiaSu WITH (UPDLOCK,HOLDLOCK) WHERE GiaSuId={owner}").ToListAsync();
            if (profiles.SingleOrDefault()?.TrangThaiDuyet != ProfileReviewState.Approved) throw new InvalidOperationException("Hồ sơ chưa được duyệt hoặc trạng thái vừa thay đổi.");
        }
        var need = form.Id.HasValue ? await db.Needs.SingleOrDefaultAsync(x => x.Id == form.Id && x.ChuSoHuuId == owner) : null;
        if (form.Id.HasValue && need == null) throw new InvalidOperationException("Không tìm thấy nhu cầu của bạn.");
        if (need == null)
        {
            need = new LearningNeed { Id = Guid.NewGuid(), ChuSoHuuId = owner, LoaiNhuCau = workspace.IsTutor ? 1 : 0, TaoLuc = DateTimeOffset.UtcNow };
            db.Needs.Add(need);
        }
        else
        {
            byte[] version;
            try { version = Convert.FromBase64String(form.Version ?? ""); } catch (FormatException) { throw new InvalidOperationException("Phiên bản dữ liệu không hợp lệ. Vui lòng tải lại."); }
            if (!version.SequenceEqual(need.RowVersion)) throw new InvalidOperationException("Nhu cầu đã thay đổi. Vui lòng tải lại trước khi sửa.");
        }
        need.MonHocId = form.SubjectId; need.KhoiLop = form.Grade; need.MucTieu = form.Goal.Trim();
        need.SoBuoiTuan = form.WeeklySessions; need.HinhThucHoc = form.Format; need.KhuVuc = form.Format == 0 ? null : form.Region?.Trim();
        need.DiaChiChiTiet = form.Format == 0 ? null : form.DetailedAddress?.Trim();
        need.HocPhiDuKien = form.Fee; need.DonViHocPhi = form.FeeUnit;
        need.TenNguoiHoc = workspace.IsParent ? form.LearnerName?.Trim() : null;
        need.TuoiNguoiHoc = workspace.IsParent ? form.LearnerAge : null;
        need.TrangThai = form.Publish ? NeedState.Open : NeedState.Draft;
        await db.SaveChangesAsync();
        await db.Availability.Where(x => x.NhuCauId == need.Id).ExecuteDeleteAsync();
        db.Availability.AddRange(form.Days.Distinct().Select(day => new AvailabilitySlot { Id = Guid.NewGuid(), NhuCauId = need.Id, ThuTrongTuan = day, GioBatDau = form.Start, GioKetThuc = form.End }));
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }

    public async Task DongNhuCauAsync(Guid id, Guid owner)
    {
        await db.Needs.Where(x => x.Id == id && x.ChuSoHuuId == owner)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, NeedState.Closed));
    }

}
