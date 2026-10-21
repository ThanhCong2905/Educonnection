using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class HoSoGiaSuService(AppDbContext db, IWebHostEnvironment? environment = null)
{
    public async Task<TutorProfileForm> LayDeSuaAsync(WorkspaceViewModel workspace)
    {
        var p=workspace.MyProfile;
        if(p==null) return new() {DisplayName=await db.Users.Where(x=>x.Id==workspace.UserId).Select(x=>x.HoTen).SingleAsync()};
        var draft=await db.TutorProfileRevisions.AsNoTracking().Where(x=>x.HoSoGiaSuId==p.Id).OrderByDescending(x=>x.TaoLuc).ThenByDescending(x=>x.Id).FirstOrDefaultAsync();
        if(draft?.TrangThai==ProfileReviewState.Approved)draft=null;
        var subjects=draft==null ? await db.TeachingSubjects.AsNoTracking().Where(x=>x.HoSoGiaSuId==p.Id).ToListAsync()
            : JsonSerializer.Deserialize<List<TeachingSubject>>(draft.MonDayJson)??[];
        return new() {Version=Convert.ToBase64String(p.RowVersion),RevisionVersion=draft?.TrangThai==ProfileReviewState.Pending?Convert.ToBase64String(draft.RowVersion):null,
            DisplayName=draft?.TenHienThi??p.TenHienThi,CurrentAvatar=draft?.AnhDaiDien??p.AnhDaiDien,School=draft?.TruongDaoTao??p.TruongDaoTao,
            Major=draft?.ChuyenNganh??p.ChuyenNganh,StudyStatus=draft?.TinhTrangHocTap??p.TinhTrangHocTap,
            Qualification=draft?.TrinhDo??p.TrinhDo,Expertise=draft?.ChuyenMon??p.ChuyenMon,Experience=draft?.KinhNghiem??p.KinhNghiem,
            Introduction=draft?.MoTa??p.MoTa,Subjects=subjects.Select(x=>x.MonHocId).ToArray(),TeachingScope=subjects.FirstOrDefault()?.PhamViDay??""};
    }

    public async Task CapNhatAsync(Guid owner,TutorProfileForm form)
    {
        var errors=new List<ValidationResult>();
        if(!Validator.TryValidateObject(form,new ValidationContext(form),errors,true)) throw new InvalidOperationException(errors[0].ErrorMessage);
        if(form.Subjects==null || form.Subjects.Length==0 || form.Subjects.Length>50 ||
           await db.Subjects.CountAsync(x=>form.Subjects.Contains(x.Id)&&x.DangSuDung)!=form.Subjects.Distinct().Count())
            throw new InvalidOperationException("Chọn ít nhất một môn học hợp lệ.");
        await using var tx=await db.Database.BeginTransactionAsync();
        var users=await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.AspNetUsers WITH (UPDLOCK,HOLDLOCK) WHERE Id={owner}").ToListAsync();
        if(users.SingleOrDefault()?.TrangThai!=0) throw new InvalidOperationException("Tài khoản không còn hoạt động.");
        if(!await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==owner && r.Name=="GiaSu" select ur).AnyAsync())
            throw new InvalidOperationException("Chỉ gia sư được cập nhật hồ sơ chuyên môn.");
        var profiles=await db.TutorProfiles.FromSqlInterpolated($"SELECT * FROM dbo.HoSoGiaSu WITH (UPDLOCK,HOLDLOCK) WHERE GiaSuId={owner}").ToListAsync();
        var p=profiles.SingleOrDefault();
        if(p==null) {
            if(!string.IsNullOrEmpty(form.Version)) throw new InvalidOperationException("Hồ sơ không còn tồn tại.");
            p=new() {Id=Guid.NewGuid(),GiaSuId=owner,TaoLuc=DateTimeOffset.UtcNow,DonViHocPhi=1};db.TutorProfiles.Add(p);
        } else HoSoGiaSuRules.CheckVersion(p.RowVersion,form.Version);
        var draft=await db.TutorProfileRevisions.SingleOrDefaultAsync(x=>x.HoSoGiaSuId==p.Id&&x.TrangThai==ProfileReviewState.Pending);
        if(draft!=null) HoSoGiaSuRules.CheckVersion(draft.RowVersion,form.RevisionVersion);
        else {
            if(!string.IsNullOrEmpty(form.RevisionVersion)) throw new InvalidOperationException("Bản chờ duyệt đã được xử lý. Vui lòng tải lại trang.");
            draft=new() {Id=Guid.NewGuid(),HoSoGiaSuId=p.Id};db.TutorProfileRevisions.Add(draft);
        }
        // Never accept an avatar path supplied by a form; retain the stored revision or accept a validated upload.
        var avatar=draft.AnhDaiDien??p.AnhDaiDien;
        if(form.Avatar!=null) avatar=await SaveAvatarAsync(form.Avatar);
        if(string.IsNullOrWhiteSpace(avatar)) throw new InvalidOperationException("Vui lòng tải ảnh đại diện JPG hoặc PNG.");
        draft.TenHienThi=form.DisplayName.Trim();draft.AnhDaiDien=avatar;draft.TrinhDo=form.Qualification.Trim();
        draft.TruongDaoTao=form.School.Trim();draft.ChuyenNganh=form.Major.Trim();draft.TinhTrangHocTap=form.StudyStatus;
        draft.ChuyenMon=form.Expertise.Trim();draft.KinhNghiem=form.Experience.Trim();draft.MoTa=form.Introduction.Trim();
        draft.MonDayJson=JsonSerializer.Serialize(form.Subjects.Distinct().Select(id=>new TeachingSubject {HoSoGiaSuId=p.Id,MonHocId=id,PhamViDay=form.TeachingScope.Trim()}));
        draft.MinhChungJson=JsonSerializer.Serialize(await db.ProfileAttachments.Where(x=>x.HoSoGiaSuId==p.Id&&x.NguoiTaiLenId==owner).Select(x=>x.Id).ToListAsync());
        draft.TaoLuc=DateTimeOffset.UtcNow;
        // The first submission is private. Approved profiles stay unchanged while a revision awaits review.
        if(p.TrangThaiDuyet!=ProfileReviewState.Approved) {
            ApplyReviewedFields(p,draft);p.TrangThaiDuyet=ProfileReviewState.Pending;p.NguoiDuyetId=null;p.DuyetLuc=null;p.LyDoTuChoi=null;
        }
        await db.SaveChangesAsync();await tx.CommitAsync();
    }

    internal static void ApplyReviewedFields(TutorProfile p,TutorProfileRevision r)
    {
        p.TenHienThi=r.TenHienThi;p.AnhDaiDien=r.AnhDaiDien;p.TrinhDo=r.TrinhDo;p.TruongDaoTao=r.TruongDaoTao;
        p.ChuyenNganh=r.ChuyenNganh;p.TinhTrangHocTap=r.TinhTrangHocTap;p.ChuyenMon=r.ChuyenMon;p.KinhNghiem=r.KinhNghiem;p.MoTa=r.MoTa;
    }

    private async Task<string> SaveAvatarAsync(IFormFile avatar)
    {
        if(environment==null) throw new InvalidOperationException("Không có nơi lưu ảnh.");
        if(avatar.Length is <1 or >2000000) throw new InvalidOperationException("Ảnh đại diện tối đa 2 MB.");
        await using var source=avatar.OpenReadStream();using var buffer=new MemoryStream();
        var chunk=new byte[8192];int count;
        while((count=await source.ReadAsync(chunk))>0) {if(buffer.Length+count>2000000)throw new InvalidOperationException("Ảnh đại diện tối đa 2 MB.");await buffer.WriteAsync(chunk.AsMemory(0,count));}
        var contentType=ProfileDataService.ValidateFile(avatar.FileName,buffer.ToArray());
        if(contentType is not ("image/jpeg" or "image/png"))throw new InvalidOperationException("Ảnh đại diện phải là JPG hoặc PNG.");
        var name=Guid.NewGuid().ToString("N")+(contentType=="image/png"?".png":".jpg");
        var folder=Path.Combine(environment.ContentRootPath,"wwwroot","images","tutors");Directory.CreateDirectory(folder);
        await File.WriteAllBytesAsync(Path.Combine(folder,name),buffer.ToArray());
        return "/images/tutors/"+name;
    }

    public async Task<ProfileDetailViewModel?> LayHoSoAsync(Guid tutorId,WorkspaceViewModel workspace,Guid? needId)
    {
        var own=workspace.IsTutor&&workspace.UserId==tutorId;
        var p=await (own?db.TutorProfiles.AsNoTracking():HoSoGiaSuRules.PublicProfiles(db)).SingleOrDefaultAsync(x=>x.GiaSuId==tutorId);
        if(p==null)return null;
        var model=new ProfileDetailViewModel {Workspace=workspace,Profile=p,Name=p.TenHienThi,SelectedNeedId=null,
            Subjects=await (from t in db.TeachingSubjects join s in db.Subjects on t.MonHocId equals s.Id where t.HoSoGiaSuId==p.Id&&s.DangSuDung select s.TenMon+" · "+t.PhamViDay).ToListAsync(),
            Conditions=await db.TutorConditions.AsNoTracking().SingleOrDefaultAsync(x=>x.HoSoGiaSuId==p.Id),
            Availability=await db.TutorAvailability.AsNoTracking().Where(x=>x.HoSoGiaSuId==p.Id).OrderBy(x=>x.ThuTrongTuan).ThenBy(x=>x.GioBatDau).ToListAsync()};
        if(own) model.LatestRevision=await db.TutorProfileRevisions.AsNoTracking().Where(x=>x.HoSoGiaSuId==p.Id).OrderByDescending(x=>x.TaoLuc).FirstOrDefaultAsync();
        else if(!workspace.IsTutor) model.Contact=await db.ContactRequests.AsNoTracking().SingleOrDefaultAsync(x=>x.GiaSuId==tutorId&&x.BenHocId==workspace.UserId);
        return model;
    }
}
