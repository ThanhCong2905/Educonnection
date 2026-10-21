using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class TimKiemService(NhuCauService nhuCau, AppDbContext db, IConfiguration? configuration=null, IWebHostEnvironment? environment=null)
{
    private bool BypassApproval=>environment?.IsDevelopment()==true&&configuration?.GetValue<bool>("Workspace:BypassTutorApproval")==true;
    public async Task TimKiemAsync(WorkspaceViewModel workspace)
    {
        if(!workspace.IsTutor) {await TimGiaSuAsync(workspace);return;}
        var f=workspace.Search;
        var q=nhuCau.Catalog().Where(x=>x.State==NeedState.Open);
        if(!string.IsNullOrWhiteSpace(f.Q)) {var word=f.Q.Trim();q=q.Where(x=>x.OwnerName.Contains(word)||x.Subject.Contains(word)||(x.Goal!=null&&x.Goal.Contains(word)));}
        if(f.SubjectId.HasValue) {var subject=workspace.Subjects.FirstOrDefault(x=>x.Id==f.SubjectId)?.TenMon;q=q.Where(x=>x.Subject==subject);}
        if(!string.IsNullOrWhiteSpace(f.Grade))q=q.Where(x=>x.Grade==f.Grade);
        if(!string.IsNullOrWhiteSpace(f.Region))q=q.Where(x=>x.Region!=null&&x.Region.Contains(f.Region));
        if(f.Format.HasValue)q=q.Where(x=>x.Format==f.Format);
        if(f.FeeUnit.HasValue)q=q.Where(x=>x.FeeUnit==f.FeeUnit);
        if(f.MaxFee.HasValue)q=q.Where(x=>x.FeeUnit==f.FeeUnit&&x.Fee<=f.MaxFee);
        workspace.Total=await q.CountAsync();f.Page=Math.Clamp(f.Page,1,workspace.PageCount);
        workspace.Cards=await q.OrderByDescending(x=>x.Created).ThenBy(x=>x.Id).Skip((f.Page-1)*9).Take(9).ToListAsync();
    }

    private async Task TimGiaSuAsync(WorkspaceViewModel workspace)
    {
        var f=workspace.Search;
        var profiles=HoSoGiaSuRules.PublicProfiles(db,BypassApproval);
        if(!string.IsNullOrWhiteSpace(f.Q)) {var word=f.Q.Trim();profiles=profiles.Where(p=>p.TenHienThi.Contains(word)||p.ChuyenMon.Contains(word)||p.MoTa.Contains(word)||db.TeachingSubjects.Any(t=>t.HoSoGiaSuId==p.Id&&db.Subjects.Any(s=>s.Id==t.MonHocId&&s.TenMon.Contains(word))));}
        if(f.SubjectId.HasValue)profiles=profiles.Where(p=>db.TeachingSubjects.Any(t=>t.HoSoGiaSuId==p.Id&&t.MonHocId==f.SubjectId));
        if(!string.IsNullOrWhiteSpace(f.Grade)) {
            var grade=f.Grade;
            profiles=profiles.Where(p=>db.TeachingSubjects.Any(t=>t.HoSoGiaSuId==p.Id&&(t.PhamViDay==grade||t.PhamViDay.StartsWith(grade+",")||t.PhamViDay.EndsWith(", "+grade)||t.PhamViDay.Contains(", "+grade+","))));
        }
        if(!string.IsNullOrWhiteSpace(f.Qualification))profiles=profiles.Where(p=>p.TrinhDo.Contains(f.Qualification));
        if(f.Day.HasValue||f.From.HasValue||f.To.HasValue)profiles=profiles.Where(p=>db.TutorAvailability.Any(s=>s.HoSoGiaSuId==p.Id&&(!f.Day.HasValue||s.ThuTrongTuan==f.Day)&&(!f.From.HasValue||s.GioBatDau<=f.From)&&(!f.To.HasValue||s.GioKetThuc>=f.To)));
        var q=from p in profiles join c in db.TutorConditions on p.Id equals c.HoSoGiaSuId where c.DangNhanHocVien select new {p,c};
        if(!string.IsNullOrWhiteSpace(f.Region))q=q.Where(x=>x.c.KhuVuc!=null&&x.c.KhuVuc.Contains(f.Region));
        if(f.Format==0)q=q.Where(x=>x.c.DayTrucTuyen);
        if(f.Format==1)q=q.Where(x=>x.c.DayTrucTiep);
        if(f.FeeUnit.HasValue)q=q.Where(x=>x.c.DonViHocPhi==f.FeeUnit);
        if(f.MaxFee.HasValue)q=q.Where(x=>x.c.DonViHocPhi==f.FeeUnit&&x.c.HocPhi<=f.MaxFee);
        workspace.Total=await q.CountAsync();f.Page=Math.Clamp(f.Page,1,workspace.PageCount);
        workspace.Cards=await q.OrderByDescending(x=>x.p.DuyetLuc).ThenBy(x=>x.p.Id).Skip((f.Page-1)*9).Take(9).Select(x=>new NeedCard {
            Id=x.p.Id,OwnerId=x.p.GiaSuId,OwnerName=x.p.TenHienThi,Avatar=x.p.AnhDaiDien,IsTutor=true,IsTutorApproved=x.p.TrangThaiDuyet==ProfileReviewState.Approved,
            State=NeedState.Open,Goal=x.p.MoTa,Region=x.c.KhuVuc,Fee=x.c.HocPhi,FeeUnit=x.c.DonViHocPhi,
            SupportsOnline=x.c.DayTrucTuyen,SupportsInPerson=x.c.DayTrucTiep,Format=x.c.DayTrucTuyen?0:1,
            Qualification=x.p.TrinhDo,Experience=x.p.KinhNghiem,Created=x.p.TaoLuc
        }).ToListAsync();
        var ids=workspace.Cards.Select(x=>x.Id).ToArray();
        var subjects=await (from t in db.TeachingSubjects join s in db.Subjects on t.MonHocId equals s.Id where ids.Contains(t.HoSoGiaSuId)&&s.DangSuDung select new {t.HoSoGiaSuId,s.TenMon,t.PhamViDay}).ToListAsync();
        foreach(var card in workspace.Cards) {card.Subject=string.Join(", ",subjects.Where(x=>x.HoSoGiaSuId==card.Id).Select(x=>x.TenMon).Distinct());card.Grade=string.Join("; ",subjects.Where(x=>x.HoSoGiaSuId==card.Id).Select(x=>x.PhamViDay).Distinct());}
    }
}
