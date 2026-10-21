using System.Text.Json;
using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class KiemDuyetHoSoService(AppDbContext db,ProfileDataService files)
{
    private async Task EnsureReviewerAsync(Guid actor)
    {
        if(!await (from u in db.Users join ur in db.UserRoles on u.Id equals ur.UserId join r in db.Roles on ur.RoleId equals r.Id
            where u.Id==actor&&u.TrangThai==0&&(r.Name=="Admin"||r.Name=="QuanTriVien") select u.Id).AnyAsync())
            throw new InvalidOperationException("Bạn không có quyền xét duyệt hồ sơ.");
    }

    public async Task<List<ProfileReviewItem>> LayDanhSachAsync(Guid actor)
    {
        await EnsureReviewerAsync(actor);
        var revisions=await db.TutorProfileRevisions.AsNoTracking().Where(x=>x.TrangThai==ProfileReviewState.Pending).OrderBy(x=>x.TaoLuc).Take(100).ToListAsync();
        var result=new List<ProfileReviewItem>();
        foreach(var revision in revisions) {
            var p=await db.TutorProfiles.AsNoTracking().SingleAsync(x=>x.Id==revision.HoSoGiaSuId);
            var ids=JsonSerializer.Deserialize<List<Guid>>(revision.MinhChungJson)??[];
            var evidence=await db.ProfileAttachments.AsNoTracking().Where(x=>ids.Contains(x.Id)&&x.HoSoGiaSuId==p.Id&&x.NguoiTaiLenId==p.GiaSuId).ToListAsync();
            var teaching=JsonSerializer.Deserialize<List<TeachingSubject>>(revision.MonDayJson)??[];
            var subjectIds=teaching.Select(x=>x.MonHocId).ToArray();
            var names=await db.Subjects.Where(x=>subjectIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.TenMon);
            result.Add(new(revision,revision.TenHienThi,evidence,teaching.Select(x=>(names.GetValueOrDefault(x.MonHocId)??"Môn đã ngừng sử dụng")+" · "+x.PhamViDay).ToList()));
        }
        return result;
    }

    public async Task DuyetAsync(Guid actor,Guid id,bool approve,string version,string? reason)
    {
        await EnsureReviewerAsync(actor);
        if(!approve&&(string.IsNullOrWhiteSpace(reason)||reason.Length>1000))throw new InvalidOperationException("Nhập lý do từ chối tối đa 1000 ký tự.");
        await using var tx=await db.Database.BeginTransactionAsync();
        var lookup=await db.TutorProfileRevisions.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id)??throw new InvalidOperationException("Không tìm thấy bản chờ duyệt.");
        var owner=await db.TutorProfiles.Where(x=>x.Id==lookup.HoSoGiaSuId).Select(x=>x.GiaSuId).SingleAsync();
        if(owner==actor)throw new InvalidOperationException("Không được tự duyệt hồ sơ của mình.");
        foreach(var userId in new[]{actor,owner}.OrderBy(x=>x)) {
            var users=await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.AspNetUsers WITH (UPDLOCK,HOLDLOCK) WHERE Id={userId}").ToListAsync();
            if(users.SingleOrDefault()?.TrangThai!=0)throw new InvalidOperationException("Tài khoản không còn hoạt động.");
        }
        var profiles=await db.TutorProfiles.FromSqlInterpolated($"SELECT * FROM dbo.HoSoGiaSu WITH (UPDLOCK,HOLDLOCK) WHERE GiaSuId={owner}").ToListAsync();
        var profile=profiles.Single();await db.Entry(profile).ReloadAsync();
        var revision=await db.TutorProfileRevisions.SingleAsync(x=>x.Id==id);await db.Entry(revision).ReloadAsync();
        HoSoGiaSuRules.CheckVersion(revision.RowVersion,version);
        if(revision.TrangThai!=ProfileReviewState.Pending)throw new InvalidOperationException("Bản hồ sơ đã được xử lý.");
        if(approve) {
            var ids=JsonSerializer.Deserialize<List<Guid>>(revision.MinhChungJson)??[];
            if(ids.Count==0||await db.ProfileAttachments.CountAsync(x=>ids.Contains(x.Id)&&x.HoSoGiaSuId==profile.Id&&x.NguoiTaiLenId==owner)!=ids.Distinct().Count())
                throw new InvalidOperationException("Hồ sơ cần minh chứng còn tồn tại và đúng chủ sở hữu trước khi duyệt.");
            var subjects=JsonSerializer.Deserialize<List<TeachingSubject>>(revision.MonDayJson)??[];
            var subjectIds=subjects.Select(x=>x.MonHocId).Distinct().ToArray();
            if(subjectIds.Length==0||await db.Subjects.CountAsync(x=>subjectIds.Contains(x.Id)&&x.DangSuDung)!=subjectIds.Length)throw new InvalidOperationException("Môn dạy không còn hợp lệ.");
            if(string.IsNullOrWhiteSpace(revision.TruongDaoTao)||string.IsNullOrWhiteSpace(revision.ChuyenNganh)||string.IsNullOrWhiteSpace(revision.AnhDaiDien))throw new InvalidOperationException("Hồ sơ chưa đủ thông tin chuyên môn.");
            HoSoGiaSuService.ApplyReviewedFields(profile,revision);
            await db.TeachingSubjects.Where(x=>x.HoSoGiaSuId==profile.Id).ExecuteDeleteAsync();
            db.TeachingSubjects.AddRange(subjects.GroupBy(x=>x.MonHocId).Select(g=>new TeachingSubject {HoSoGiaSuId=profile.Id,MonHocId=g.Key,PhamViDay=g.First().PhamViDay}));
        }
        var state=approve?ProfileReviewState.Approved:ProfileReviewState.Rejected;
        revision.TrangThai=state;revision.NguoiDuyetId=actor;revision.DuyetLuc=DateTimeOffset.UtcNow;revision.LyDoTuChoi=approve?null:reason!.Trim();
        if(approve||profile.TrangThaiDuyet!=ProfileReviewState.Approved) {
            profile.TrangThaiDuyet=state;profile.NguoiDuyetId=actor;profile.DuyetLuc=revision.DuyetLuc;profile.LyDoTuChoi=revision.LyDoTuChoi;
        }
        await db.SaveChangesAsync();await tx.CommitAsync();
    }

    public async Task<(ProfileAttachment Metadata,byte[] Bytes)?> TaiMinhChungAsync(Guid actor,Guid revisionId,Guid fileId)
    {
        await EnsureReviewerAsync(actor);
        var revision=await db.TutorProfileRevisions.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==revisionId);
        if(revision==null||!(JsonSerializer.Deserialize<List<Guid>>(revision.MinhChungJson)??[]).Contains(fileId))return null;
        var owner=await db.TutorProfiles.Where(x=>x.Id==revision.HoSoGiaSuId).Select(x=>x.GiaSuId).SingleAsync();
        return await files.DownloadAsync(owner,fileId);
    }
}
