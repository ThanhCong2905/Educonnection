using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class HoSoGiaSuService(AppDbContext db, NhuCauService nhuCau, IConfiguration? configuration = null, IWebHostEnvironment? environment = null)
{
    private bool BypassApproval => environment?.IsDevelopment() == true && configuration?.GetValue<bool>("Workspace:BypassTutorApproval") == true;
    private async Task EnsureActiveAsync(Guid owner)
    {
        var accounts = await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.AspNetUsers WITH (UPDLOCK,HOLDLOCK) WHERE Id={owner}").ToListAsync();
        if (accounts.SingleOrDefault()?.TrangThai != 0) throw new InvalidOperationException("Tài khoản không còn hoạt động. Vui lòng đăng nhập lại.");
    }

    public async Task<TutorProfileForm> LayDeSuaAsync(WorkspaceViewModel workspace)
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

    public async Task CapNhatAsync(Guid owner, TutorProfileForm form)
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

    public async Task<ProfileDetailViewModel?> LayHoSoAsync(Guid tutorId, WorkspaceViewModel workspace, Guid? needId)
    {
        var own = workspace.IsTutor && workspace.MyProfile?.GiaSuId == tutorId;
        var profile = await db.TutorProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.GiaSuId == tutorId && (own || BypassApproval || x.TrangThaiDuyet == ProfileReviewState.Approved));
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == tutorId && x.TrangThai == 0);
        if (profile == null || user == null) return null;
        return new ProfileDetailViewModel { Workspace = workspace, Profile = profile, Name = user.HoTen,
            SelectedNeedId = needId,
            Subjects = await (from t in db.TeachingSubjects join s in db.Subjects on t.MonHocId equals s.Id where t.HoSoGiaSuId == profile.Id select s.TenMon + " · " + t.PhamViDay).ToListAsync(),
            Needs = await nhuCau.Catalog(true).Where(x => x.OwnerId == tutorId && x.State == NeedState.Open).OrderByDescending(x => x.Created).ToListAsync() };
    }

}
