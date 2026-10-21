using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class QuyenTuongTacService(AppDbContext db, IConfiguration configuration, IWebHostEnvironment environment)
{
    private bool Bypass => environment.IsDevelopment() && configuration.GetValue<bool>("Workspace:BypassTutorApproval");
    internal async Task LockAsync(string resource) => await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result=sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000; IF @result<0 THROW 50001, 'Please retry this operation.', 1;");

    private async Task<string[]> RolesAsync(Guid id) => await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==id select r.Name!).ToArrayAsync();

    internal async Task<(Guid Tutor,Guid Learner)> PairAsync(Guid actor,Guid target)
    {
        if(actor==target) throw new InvalidOperationException("Không thể tự kết nối hoặc nhắn tin với chính mình.");
        // Consistent lock order serializes writes against account locks and profile changes.
        foreach(var id in new[]{actor,target}.OrderBy(x=>x)) {
            var users=await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.AspNetUsers WITH (UPDLOCK,HOLDLOCK) WHERE Id={id}").ToListAsync();
            if(users.SingleOrDefault()?.TrangThai!=0) throw new InvalidOperationException("Một trong hai tài khoản không còn hoạt động.");
        }
        var roles=await RolesAsync(actor); var targetRoles=await RolesAsync(target);
        Guid tutor,learner;
        if(roles.Contains("GiaSu") && targetRoles.Any(x=>x is "HocSinh" or "PhuHuynh")) {tutor=actor;learner=target;}
        else if(targetRoles.Contains("GiaSu") && roles.Any(x=>x is "HocSinh" or "PhuHuynh")) {tutor=target;learner=actor;}
        else throw new InvalidOperationException("Kết nối chỉ áp dụng giữa gia sư và học sinh/phụ huynh.");
        foreach(var id in new[]{actor,target})
            if(!await db.PersonalProfiles.AnyAsync(x=>x.TaiKhoanId==id && x.ThanhPho.Trim()!="" && x.DiaChiChiTiet!=null && x.DiaChiChiTiet.Trim()!=""))
                throw new InvalidOperationException(id==actor?"Bạn cần hoàn thiện địa chỉ trong hồ sơ cá nhân.":"Đối phương chưa hoàn thiện hồ sơ cá nhân.");
        var profiles=await db.TutorProfiles.FromSqlInterpolated($"SELECT * FROM dbo.HoSoGiaSu WITH (UPDLOCK,HOLDLOCK) WHERE GiaSuId={tutor}").ToListAsync();
        var profile=profiles.SingleOrDefault();
        if(profile==null || (!Bypass && profile.TrangThaiDuyet!=ProfileReviewState.Approved)) throw new InvalidOperationException("Hồ sơ gia sư cần được xét duyệt trước khi kết nối hoặc nhắn tin.");
        if(!await db.ProfileAttachments.AnyAsync(x=>x.HoSoGiaSuId==profile.Id && x.NguoiTaiLenId==tutor)) throw new InvalidOperationException("Gia sư cần bổ sung tệp bằng cấp/chứng chỉ.");
        return(tutor,learner);
    }

}
