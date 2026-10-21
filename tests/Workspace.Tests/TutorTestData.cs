using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.WorkspaceTests;

internal static class TutorTestData
{
    internal static async Task Role(AppDbContext db,Guid user,string name)
    {
        var role=await db.Roles.SingleOrDefaultAsync(x=>x.Name==name);
        if(role==null){role=new IdentityRole<Guid>{Id=Guid.NewGuid(),Name=name,NormalizedName=name.ToUpperInvariant()};db.Roles.Add(role);await db.SaveChangesAsync();}
        if(!await db.UserRoles.AnyAsync(x=>x.UserId==user)) {db.UserRoles.Add(new(){UserId=user,RoleId=role.Id});await db.SaveChangesAsync();}
    }

    internal static async Task Complete(AppDbContext db,TutorProfile p,int subject,Guid reviewer,bool approved=true)
    {
        await Role(db,p.GiaSuId,"GiaSu");
        p.TenHienThi="Gia sư kiểm thử";p.AnhDaiDien="/images/auth/logo.png";p.TruongDaoTao="Đại học kiểm thử";p.ChuyenNganh="Sư phạm Toán";p.TinhTrangHocTap="DaTotNghiep";
        p.TrangThaiDuyet=approved?ProfileReviewState.Approved:ProfileReviewState.Pending;p.NguoiDuyetId=approved?reviewer:null;p.DuyetLuc=approved?DateTimeOffset.UtcNow:null;
        await db.SaveChangesAsync();
        if(!await db.TeachingSubjects.AnyAsync(x=>x.HoSoGiaSuId==p.Id))db.TeachingSubjects.Add(new(){HoSoGiaSuId=p.Id,MonHocId=subject,PhamViDay="Lớp 9"});
        if(!await db.ProfileAttachments.AnyAsync(x=>x.HoSoGiaSuId==p.Id))db.ProfileAttachments.Add(new(){Id=Guid.NewGuid(),NguoiTaiLenId=p.GiaSuId,HoSoGiaSuId=p.Id,TenGoc="bang-cap.pdf",TenLuuTru=Guid.NewGuid().ToString("N"),LoaiNoiDung="application/pdf",KichThuoc=10,Checksum=new string('a',64),TaoLuc=DateTimeOffset.UtcNow});
        if(!await db.TutorConditions.AnyAsync(x=>x.HoSoGiaSuId==p.Id))db.TutorConditions.Add(new(){HoSoGiaSuId=p.Id,DayTrucTuyen=true,DangNhanHocVien=true,HocPhi=200000,DonViHocPhi=1,CapNhatLuc=DateTimeOffset.UtcNow});
        await db.SaveChangesAsync();
        if(!await db.TutorAvailability.AnyAsync(x=>x.HoSoGiaSuId==p.Id))db.TutorAvailability.Add(new(){Id=Guid.NewGuid(),HoSoGiaSuId=p.Id,ThuTrongTuan=1,GioBatDau=new(18,0),GioKetThuc=new(20,0)});
        await db.SaveChangesAsync();
    }
}
