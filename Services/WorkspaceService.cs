using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class WorkspaceService(AppDbContext db, IConfiguration? configuration = null, IWebHostEnvironment? environment = null)
{
    private bool BypassApproval => environment?.IsDevelopment() == true && configuration?.GetValue<bool>("Workspace:BypassTutorApproval") == true;
    private async Task EnsureActiveAsync(Guid owner)
    {
        var accounts = await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.AspNetUsers WITH (UPDLOCK,HOLDLOCK) WHERE Id={owner}").ToListAsync();
        if (accounts.SingleOrDefault()?.TrangThai != 0) throw new InvalidOperationException("Tài khoản không còn hoạt động. Vui lòng đăng nhập lại.");
    }
    public async Task<MyAccountViewModel> MyAccountAsync(Guid owner, WorkspaceViewModel workspace)
    {
        return await db.Users.AsNoTracking().Where(x => x.Id == owner)
            .Select(x => new MyAccountViewModel(workspace, x.HoTen, x.Email, x.PhoneNumber)).SingleAsync();
    }

    public async Task<TutorProfileForm> ProfileFormAsync(WorkspaceViewModel workspace)
    {
        var p = workspace.MyProfile;
        if (p == null) {
            return new();
        }
        var subjects = await db.TeachingSubjects.AsNoTracking().Where(x => x.HoSoGiaSuId == p.Id).ToListAsync();
        return new() { Version = Convert.ToBase64String(p.RowVersion), Qualification = p.TrinhDo, Expertise = p.ChuyenMon,
            Experience = p.KinhNghiem, Introduction = p.MoTa, Region = p.KhuVuc, Fee = p.HocPhiCongKhai,
            FeeUnit = p.DonViHocPhi, Subjects = subjects.Select(x => x.MonHocId).ToArray(), TeachingScope = subjects.FirstOrDefault()?.PhamViDay ?? "" };
    }

    public async Task SaveProfileAsync(Guid owner, TutorProfileForm form)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await EnsureActiveAsync(owner);
        var profiles = await db.TutorProfiles.FromSqlInterpolated($"SELECT * FROM dbo.HoSoGiaSu WITH (UPDLOCK,HOLDLOCK) WHERE GiaSuId={owner}").ToListAsync();
        var profile = profiles.SingleOrDefault();
        if (profile == null)
        {
            if (!string.IsNullOrEmpty(form.Version)) throw new InvalidOperationException("Hồ sơ không còn tồn tại. Vui lòng tải lại.");
            profile = new TutorProfile { Id = Guid.NewGuid(), GiaSuId = owner, TaoLuc = DateTimeOffset.UtcNow };
            db.TutorProfiles.Add(profile);
        }
        else
        {
            byte[] version;
            try { version = Convert.FromBase64String(form.Version ?? ""); } catch(FormatException) { throw new InvalidOperationException("Phiên bản hồ sơ không hợp lệ."); }
            if (!version.SequenceEqual(profile.RowVersion)) throw new InvalidOperationException("Hồ sơ đã thay đổi. Vui lòng tải lại trang.");
        }
        profile.TrinhDo = form.Qualification.Trim(); profile.ChuyenMon = form.Expertise.Trim();
        profile.KinhNghiem = form.Experience.Trim(); profile.MoTa = form.Introduction.Trim(); profile.KhuVuc = form.Region.Trim();
        profile.HocPhiCongKhai = form.Fee; profile.DonViHocPhi = form.FeeUnit;
        profile.TrangThaiDuyet = ProfileReviewState.Pending; profile.NguoiDuyetId = null; profile.DuyetLuc = null; profile.LyDoTuChoi = null;
        await db.SaveChangesAsync();
        await db.TeachingSubjects.Where(x => x.HoSoGiaSuId == profile.Id).ExecuteDeleteAsync();
        db.TeachingSubjects.AddRange(form.Subjects.Distinct().Select(id => new TeachingSubject { HoSoGiaSuId = profile.Id, MonHocId = id, PhamViDay = form.TeachingScope.Trim() }));
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }
    public async Task<WorkspaceViewModel> GetWorkspaceAsync(Guid userId, bool tutor, bool parent)
    {
        return new WorkspaceViewModel
        {
            IsTutor = tutor, IsParent = parent, ApprovalBypassed = BypassApproval,
            HasRequiredAddress = await db.PersonalProfiles.AnyAsync(x => x.TaiKhoanId == userId && x.ThanhPho.Trim() != "" && x.DiaChiChiTiet != null && x.DiaChiChiTiet.Trim() != ""),
            HasQualificationFile = tutor && await db.ProfileAttachments.AnyAsync(x => x.NguoiTaiLenId == userId && x.HoSoGiaSuId != null),
            MyProfile = tutor ? await db.TutorProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.GiaSuId == userId) : null,
            Subjects = await db.Subjects.AsNoTracking().Where(x => x.DangSuDung).OrderBy(x => x.TenMon).ToListAsync(),
            MyNeeds = await db.Needs.AsNoTracking().Where(x => x.ChuSoHuuId == userId).OrderByDescending(x => x.TaoLuc).ToListAsync()
        };
    }

    private IQueryable<NeedCard> Catalog(bool tutorListings)
    {
        var query = from n in db.Needs.AsNoTracking()
                    join s in db.Subjects on n.MonHocId equals s.Id
                    join u in db.Users on n.ChuSoHuuId equals u.Id
                    where u.TrangThai == 0 && s.DangSuDung
                    select new { n, s, u };
        if (tutorListings)
            return from x in query
                   join p in db.TutorProfiles on x.u.Id equals p.GiaSuId
                   where x.n.LoaiNhuCau == 1 && (BypassApproval || p.TrangThaiDuyet == ProfileReviewState.Approved)
                   select new NeedCard { Id = x.n.Id, OwnerId = x.u.Id, OwnerName = x.u.HoTen, Subject = x.s.TenMon,
                       Grade = x.n.KhoiLop, Goal = x.n.MucTieu, Region = x.n.KhuVuc, Format = x.n.HinhThucHoc,
                       Fee = x.n.HocPhiDuKien, FeeUnit = x.n.DonViHocPhi, WeeklySessions = x.n.SoBuoiTuan,
                       IsTutor = true, IsTutorApproved = p.TrangThaiDuyet == ProfileReviewState.Approved, Qualification = p.TrinhDo, Experience = p.KinhNghiem, Created = x.n.TaoLuc, State = x.n.TrangThai };
        return query.Where(x => x.n.LoaiNhuCau == 0).Select(x => new NeedCard { Id = x.n.Id, OwnerId = x.u.Id,
            OwnerName = x.u.HoTen, Subject = x.s.TenMon, Grade = x.n.KhoiLop, Goal = x.n.MucTieu,
            Region = x.n.KhuVuc, Format = x.n.HinhThucHoc, Fee = x.n.HocPhiDuKien, FeeUnit = x.n.DonViHocPhi,
            WeeklySessions = x.n.SoBuoiTuan, Created = x.n.TaoLuc, State = x.n.TrangThai });
    }

    public async Task SearchAsync(WorkspaceViewModel workspace)
    {
        var f = workspace.Search;
        var q = Catalog(!workspace.IsTutor).Where(x => x.State == NeedState.Open);
        if (!string.IsNullOrWhiteSpace(f.Q)) { var word = f.Q.Trim(); q = q.Where(x => x.OwnerName.Contains(word) || x.Subject.Contains(word) || (x.Goal != null && x.Goal.Contains(word))); }
        if (f.SubjectId.HasValue)
        {
            var subject = workspace.Subjects.FirstOrDefault(x => x.Id == f.SubjectId)?.TenMon;
            q = q.Where(x => x.Subject == subject);
        }
        if (!string.IsNullOrWhiteSpace(f.Grade)) q = q.Where(x => x.Grade == f.Grade);
        if (!string.IsNullOrWhiteSpace(f.Region)) q = q.Where(x => x.Region != null && x.Region.Contains(f.Region));
        if (f.Format.HasValue) q = q.Where(x => x.Format == f.Format);
        if (f.FeeUnit.HasValue) q = q.Where(x => x.FeeUnit == f.FeeUnit);
        if (f.MaxFee.HasValue) { var unit = f.FeeUnit ?? 1; q = q.Where(x => x.FeeUnit == unit && x.Fee <= f.MaxFee); }
        workspace.Total = await q.CountAsync();
        f.Page = Math.Clamp(f.Page, 1, workspace.PageCount);
        workspace.Cards = await q.OrderByDescending(x => x.Created).ThenBy(x => x.Id).Skip((f.Page - 1) * 9).Take(9).ToListAsync();
    }

    public async Task<List<NeedCard>> MyCardsAsync(Guid userId)
    {
        return await (from n in db.Needs.AsNoTracking() join s in db.Subjects on n.MonHocId equals s.Id
                      where n.ChuSoHuuId == userId orderby n.TaoLuc descending
                      select new NeedCard { Id = n.Id, OwnerId = userId, Subject = s.TenMon, Grade = n.KhoiLop,
                          Goal = n.MucTieu, Format = n.HinhThucHoc, Region = n.KhuVuc, Fee = n.HocPhiDuKien,
                          FeeUnit = n.DonViHocPhi, WeeklySessions = n.SoBuoiTuan, State = n.TrangThai,
                          IsTutor = n.LoaiNhuCau == 1, Created = n.TaoLuc }).ToListAsync();
    }

    public async Task<ProfileDetailViewModel?> ProfileAsync(Guid tutorId, WorkspaceViewModel workspace, Guid? needId)
    {
        var own = workspace.IsTutor && workspace.MyProfile?.GiaSuId == tutorId;
        var profile = await db.TutorProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.GiaSuId == tutorId && (own || BypassApproval || x.TrangThaiDuyet == ProfileReviewState.Approved));
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == tutorId && x.TrangThai == 0);
        if (profile == null || user == null) return null;
        return new ProfileDetailViewModel { Workspace = workspace, Profile = profile, Name = user.HoTen,
            SelectedNeedId = needId,
            Subjects = await (from t in db.TeachingSubjects join s in db.Subjects on t.MonHocId equals s.Id where t.HoSoGiaSuId == profile.Id select s.TenMon + " · " + t.PhamViDay).ToListAsync(),
            Needs = await Catalog(true).Where(x => x.OwnerId == tutorId && x.State == NeedState.Open).OrderByDescending(x => x.Created).ToListAsync() };
    }

    public async Task<NeedCard?> NeedAsync(Guid id)
    {
        var result = await Catalog(false).SingleOrDefaultAsync(x => x.Id == id && x.State == NeedState.Open);
        if (result != null) result.Slots = await db.Availability.AsNoTracking().Where(x => x.NhuCauId == id).OrderBy(x => x.ThuTrongTuan).ToListAsync();
        return result;
    }

    public Task<LearningNeed?> OwnedNeedAsync(Guid id, Guid owner) => db.Needs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ChuSoHuuId == owner);
    public Task<List<AvailabilitySlot>> SlotsAsync(Guid id) => db.Availability.AsNoTracking().Where(x => x.NhuCauId == id).ToListAsync();

    public async Task SaveAsync(Guid owner, WorkspaceViewModel workspace, NeedForm form)
    {
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

    public async Task CloseAsync(Guid id, Guid owner)
    {
        await db.Needs.Where(x => x.Id == id && x.ChuSoHuuId == owner)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, NeedState.Closed));
    }
}
