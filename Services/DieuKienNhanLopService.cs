using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class DieuKienNhanLopService(AppDbContext db)
{
    public async Task<TutorConditionsForm> LayDeSuaAsync(Guid owner)
    {
        var p = await db.TutorProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.GiaSuId==owner);
        if(p==null) throw new InvalidOperationException("Vui lòng tạo hồ sơ chuyên môn trước.");
        var c=await db.TutorConditions.AsNoTracking().SingleOrDefaultAsync(x=>x.HoSoGiaSuId==p.Id);
        var slots=await db.TutorAvailability.AsNoTracking().Where(x=>x.HoSoGiaSuId==p.Id).OrderBy(x=>x.ThuTrongTuan).ThenBy(x=>x.GioBatDau).ToListAsync();
        return new() {Version=c==null?null:Convert.ToBase64String(c.RowVersion),Online=c?.DayTrucTuyen??true,InPerson=c?.DayTrucTiep??false,Region=c?.KhuVuc,
            Fee=c?.HocPhi,FeeUnit=c?.DonViHocPhi??1,AcceptingStudents=c?.DangNhanHocVien??false,
            Slots=Enumerable.Range(0,7).SelectMany(day=>slots.Any(s=>s.ThuTrongTuan==day)
                ? slots.Where(s=>s.ThuTrongTuan==day).Select(s=>new TutorSlotForm {Enabled=true,Day=day,Start=s.GioBatDau,End=s.GioKetThuc})
                : [new TutorSlotForm {Day=day}]).ToList()};
    }

    public async Task CapNhatAsync(Guid owner,TutorConditionsForm form)
    {
        if(!form.Online&&!form.InPerson) throw new InvalidOperationException("Chọn ít nhất một hình thức dạy.");
        if(form.InPerson && string.IsNullOrWhiteSpace(form.Region)) throw new InvalidOperationException("Nhập khu vực khi dạy trực tiếp.");
        if(form.Region?.Length>200 || form.Fee is <0 or >100000000 || form.FeeUnit is <0 or >2 || form.Slots==null || form.Slots.Count>28)
            throw new InvalidOperationException("Điều kiện nhận lớp không hợp lệ.");
        var slots=form.Slots.Where(s=>s.Enabled).ToList();
        if(form.AcceptingStudents && slots.Count==0) throw new InvalidOperationException("Chọn lịch rảnh trước khi bật nhận học viên.");
        if(slots.Any(s=>s.Day is <0 or >6 || s.Start>=s.End)) throw new InvalidOperationException("Khung giờ không hợp lệ.");
        foreach(var day in slots.GroupBy(s=>s.Day)) {
            var ordered=day.OrderBy(s=>s.Start).ToList();
            for(var i=1;i<ordered.Count;i++) if(ordered[i].Start<ordered[i-1].End) throw new InvalidOperationException("Các khung giờ trong cùng ngày không được trùng nhau.");
        }
        await using var tx=await db.Database.BeginTransactionAsync();
        var users=await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.AspNetUsers WITH (UPDLOCK,HOLDLOCK) WHERE Id={owner}").ToListAsync();
        if(users.SingleOrDefault()?.TrangThai!=0) throw new InvalidOperationException("Tài khoản không còn hoạt động.");
        var profiles=await db.TutorProfiles.FromSqlInterpolated($"SELECT * FROM dbo.HoSoGiaSu WITH (UPDLOCK,HOLDLOCK) WHERE GiaSuId={owner}").ToListAsync();
        var profile=profiles.SingleOrDefault()??throw new InvalidOperationException("Vui lòng tạo hồ sơ chuyên môn trước.");
        var condition=await db.TutorConditions.SingleOrDefaultAsync(x=>x.HoSoGiaSuId==profile.Id);
        if(condition!=null) HoSoGiaSuRules.CheckVersion(condition.RowVersion,form.Version);
        else {
            if(!string.IsNullOrEmpty(form.Version)) throw new InvalidOperationException("Điều kiện nhận lớp đã thay đổi.");
            condition=new() {HoSoGiaSuId=profile.Id};db.TutorConditions.Add(condition);
        }
        condition.DayTrucTuyen=form.Online;condition.DayTrucTiep=form.InPerson;condition.KhuVuc=form.InPerson?form.Region?.Trim():null;
        condition.HocPhi=form.Fee;condition.DonViHocPhi=form.Fee.HasValue?form.FeeUnit:null;
        condition.DangNhanHocVien=form.AcceptingStudents;condition.CapNhatLuc=DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        await db.TutorAvailability.Where(x=>x.HoSoGiaSuId==profile.Id).ExecuteDeleteAsync();
        db.TutorAvailability.AddRange(slots.Select(s=>new TutorAvailability {Id=Guid.NewGuid(),HoSoGiaSuId=profile.Id,ThuTrongTuan=s.Day,GioBatDau=s.Start,GioKetThuc=s.End}));
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
