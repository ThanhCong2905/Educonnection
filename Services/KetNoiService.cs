using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class KetNoiService(AppDbContext db, QuyenTuongTacService quyen)
{
    public Task ChapNhanDeXuatAsync(Guid actor, Guid id, string version) => PhanHoiAsync(actor, id, "accept", version);
    public Task RutYeuCauAsync(Guid actor, Guid id, string version) => PhanHoiAsync(actor, id, "cancel", version);
    public Task TuChoiDeXuatAsync(Guid actor, Guid id, string version) => PhanHoiAsync(actor, id, "reject", version);

    public Task XuLyHetHanYeuCauAsync() => db.ConnectionRequests.Where(x=>(x.TrangThai==ConnectionState.Requested || x.TrangThai==ConnectionState.Proposed) && x.HetHanLuc<=DateTimeOffset.UtcNow)
        .ExecuteUpdateAsync(set=>set.SetProperty(x=>x.TrangThai,ConnectionState.Expired));

    public async Task<Guid> GuiYeuCauAsync(Guid actor,ConnectForm form)
    {
        if(string.IsNullOrWhiteSpace(form.Note) || form.Note.Length>2000) throw new InvalidOperationException("Lời nhắn phải có từ 1 đến 2000 ký tự.");
        await XuLyHetHanYeuCauAsync(); await using var transaction=await db.Database.BeginTransactionAsync();
        var pair=await quyen.PairAsync(actor,form.Target); await quyen.LockAsync($"connection:{pair.Tutor}:{pair.Learner}");
        if(form.NeedId.HasValue) {
            var need=await db.Needs.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==form.NeedId && x.TrangThai==NeedState.Open);
            if(need==null || (need.ChuSoHuuId!=pair.Tutor && need.ChuSoHuuId!=pair.Learner) || (need.ChuSoHuuId==pair.Tutor && need.LoaiNhuCau!=1) || (need.ChuSoHuuId==pair.Learner && need.LoaiNhuCau!=0))
                throw new InvalidOperationException("Nhu cầu không thuộc hai bên hoặc không còn mở.");
        }
        var existing=await db.ConnectionRequests.SingleOrDefaultAsync(x=>x.GiaSuId==pair.Tutor && x.BenHocId==pair.Learner && x.NhuCauId==form.NeedId && (x.TrangThai==ConnectionState.Requested || x.TrangThai==ConnectionState.Proposed));
        if(existing!=null) {await transaction.CommitAsync();return existing.Id;}
        var request=new ConnectionRequest {Id=Guid.NewGuid(),GiaSuId=pair.Tutor,BenHocId=pair.Learner,NguoiGuiId=actor,NhuCauId=form.NeedId,LoiNhan=form.Note.Trim(),GuiLuc=DateTimeOffset.UtcNow,HetHanLuc=DateTimeOffset.UtcNow.AddHours(72)};
        db.ConnectionRequests.Add(request);await db.SaveChangesAsync();await transaction.CommitAsync();return request.Id;
    }

    public async Task<List<ConnectionItem>> LayDanhSachAsync(Guid actor)
    {
        await XuLyHetHanYeuCauAsync();
        return await (from r in db.ConnectionRequests.AsNoTracking() join u in db.Users on (r.GiaSuId==actor?r.BenHocId:r.GiaSuId) equals u.Id
                      where r.GiaSuId==actor || r.BenHocId==actor orderby r.GuiLuc descending select new ConnectionItem(r,u.HoTen,r.NguoiGuiId==actor)).Take(100).ToListAsync();
    }

    public Task<ConnectionRequest?> LayYeuCauAsync(Guid actor,Guid id) => db.ConnectionRequests.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id && (x.GiaSuId==actor || x.BenHocId==actor));

    private static void CheckVersion(ConnectionRequest request,string version) {
        if(Convert.ToBase64String(request.RowVersion)!=version) throw new InvalidOperationException("Yêu cầu đã thay đổi. Vui lòng tải lại trang.");
    }

    public async Task GuiDeXuatAsync(Guid actor,ClassProposalForm form)
    {
        if(string.IsNullOrWhiteSpace(form.Name)||form.Name.Length>200||string.IsNullOrWhiteSpace(form.Goal)||form.Goal.Length>2000||string.IsNullOrWhiteSpace(form.Schedule)||form.Schedule.Length>2000||form.Sessions is <1 or >7||form.Duration is <1 or >480||form.Format is <0 or >1||form.Fee is <0 or >100000000||form.FeeUnit is <0 or >2||form.Location?.Length>500 || (form.Format==1&&string.IsNullOrWhiteSpace(form.Location)))
            throw new InvalidOperationException("Vui lòng nhập đầy đủ và đúng điều kiện lớp học.");
        await XuLyHetHanYeuCauAsync();await using var transaction=await db.Database.BeginTransactionAsync();
        var request=await db.ConnectionRequests.SingleOrDefaultAsync(x=>x.Id==form.Id && x.GiaSuId==actor) ?? throw new InvalidOperationException("Chỉ gia sư của yêu cầu được đề xuất lớp.");
        await quyen.PairAsync(actor,request.BenHocId);await quyen.LockAsync($"request:{request.Id}");await db.Entry(request).ReloadAsync();CheckVersion(request,form.Version);
        if(request.TrangThai is not (ConnectionState.Requested or ConnectionState.Proposed) || request.HetHanLuc<=DateTimeOffset.UtcNow) throw new InvalidOperationException("Yêu cầu không còn chờ phản hồi.");
        if(!await db.Subjects.AnyAsync(x=>x.Id==form.SubjectId && x.DangSuDung)) throw new InvalidOperationException("Môn học không khả dụng.");
        request.TenLopDuKien=form.Name.Trim();request.MonHocId=form.SubjectId;request.MucTieu=form.Goal.Trim();request.SoBuoiTuan=form.Sessions;request.ThoiLuongPhut=form.Duration;
        request.LichDuKien=form.Schedule.Trim();request.HinhThucHoc=form.Format;request.DiaDiem=form.Format==1?form.Location?.Trim():null;request.HocPhi=form.Fee;request.DonViHocPhi=form.FeeUnit;
        request.TrangThai=ConnectionState.Proposed;request.GuiLuc=DateTimeOffset.UtcNow;request.HetHanLuc=request.GuiLuc.AddHours(72);
        await db.SaveChangesAsync();await transaction.CommitAsync();
    }

    public async Task PhanHoiAsync(Guid actor,Guid id,string action,string version)
    {
        await XuLyHetHanYeuCauAsync();await using var transaction=await db.Database.BeginTransactionAsync();
        var request=await db.ConnectionRequests.SingleOrDefaultAsync(x=>x.Id==id && (x.GiaSuId==actor || x.BenHocId==actor)) ?? throw new InvalidOperationException("Không tìm thấy yêu cầu của bạn.");
        await quyen.PairAsync(actor,request.GiaSuId==actor?request.BenHocId:request.GiaSuId);await quyen.LockAsync($"request:{id}");await db.Entry(request).ReloadAsync();CheckVersion(request,version);
        if(request.TrangThai is not (ConnectionState.Requested or ConnectionState.Proposed) || request.HetHanLuc<=DateTimeOffset.UtcNow) throw new InvalidOperationException("Yêu cầu đã được xử lý hoặc hết hạn.");
        if(action=="accept") {
            if(actor!=request.BenHocId || request.TrangThai!=ConnectionState.Proposed || request.MonHocId==null || request.HocPhi==null) throw new InvalidOperationException("Chỉ bên học được xác nhận đề xuất lớp của gia sư.");
            db.ConnectedClasses.Add(new() {Id=Guid.NewGuid(),YeuCauKetNoiId=id,GiaSuId=request.GiaSuId,BenHocId=request.BenHocId,TenLop=request.TenLopDuKien!,MonHocId=request.MonHocId.Value,
                MucTieu=request.MucTieu!,SoBuoiTuan=request.SoBuoiTuan!.Value,ThoiLuongPhut=request.ThoiLuongPhut!.Value,HinhThucHoc=request.HinhThucHoc!.Value,DiaDiem=request.DiaDiem,HocPhi=request.HocPhi.Value,DonViHocPhi=request.DonViHocPhi!.Value,TaoLuc=DateTimeOffset.UtcNow});
            request.TrangThai=ConnectionState.Accepted;
        } else if(action=="cancel" && (request.TrangThai==ConnectionState.Proposed?actor==request.GiaSuId:actor==request.NguoiGuiId)) request.TrangThai=ConnectionState.Cancelled;
        else if(action=="reject" && (request.TrangThai==ConnectionState.Proposed?actor==request.BenHocId:actor!=request.NguoiGuiId)) request.TrangThai=ConnectionState.Rejected;
        else throw new InvalidOperationException("Bạn không có quyền thực hiện thao tác này.");
        await db.SaveChangesAsync();await transaction.CommitAsync();
    }

}
