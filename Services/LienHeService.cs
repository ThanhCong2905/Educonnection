using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class LienHeService(AppDbContext db, QuyenTuongTacService quyen)
{
    public async Task<Guid> GuiAsync(Guid actor,ConnectForm form)
    {
        if(string.IsNullOrWhiteSpace(form.Note)||form.Note.Length>2000)throw new InvalidOperationException("Nhập lời nhắn liên hệ từ 1 đến 2000 ký tự.");
        await using var tx=await db.Database.BeginTransactionAsync();
        var pair=await quyen.PairAsync(actor,form.Target);
        await quyen.LockAsync($"contact:{pair.Tutor}:{pair.Learner}");
        var contact=await db.ContactRequests.SingleOrDefaultAsync(x=>x.GiaSuId==pair.Tutor&&x.BenHocId==pair.Learner);
        if(contact?.TrangThai is ContactState.Accepted or ContactState.Pending) {await tx.CommitAsync();return contact.Id;}
        if(contact==null) {contact=new() {Id=Guid.NewGuid(),GiaSuId=pair.Tutor,BenHocId=pair.Learner};db.ContactRequests.Add(contact);}
        contact.NguoiGuiId=actor;contact.LoiNhan=form.Note.Trim();contact.TrangThai=ContactState.Pending;contact.GuiLuc=DateTimeOffset.UtcNow;contact.PhanHoiLuc=null;
        await db.SaveChangesAsync();await tx.CommitAsync();return contact.Id;
    }

    public async Task PhanHoiAsync(Guid actor,Guid id,string decision,string version)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        var contact=await db.ContactRequests.SingleOrDefaultAsync(x=>x.Id==id&&(x.GiaSuId==actor||x.BenHocId==actor))??throw new InvalidOperationException("Không tìm thấy yêu cầu liên hệ của bạn.");
        if(decision=="accept") await quyen.PairAsync(actor,contact.GiaSuId==actor?contact.BenHocId:contact.GiaSuId);
        await quyen.LockAsync($"contact:{contact.GiaSuId}:{contact.BenHocId}");
        await db.Entry(contact).ReloadAsync();HoSoGiaSuRules.CheckVersion(contact.RowVersion,version);
        if(contact.TrangThai!=ContactState.Pending)throw new InvalidOperationException("Yêu cầu liên hệ đã được xử lý.");
        if(decision=="withdraw"&&actor==contact.NguoiGuiId)contact.TrangThai=ContactState.Withdrawn;
        else if(actor!=contact.NguoiGuiId&&decision is "accept" or "reject")contact.TrangThai=decision=="accept"?ContactState.Accepted:ContactState.Rejected;
        else throw new InvalidOperationException("Chỉ người nhận được chấp nhận hoặc từ chối liên hệ.");
        contact.PhanHoiLuc=DateTimeOffset.UtcNow;
        if(contact.TrangThai==ContactState.Accepted && !await db.Conversations.AnyAsync(x=>x.LoaiHoiThoai==0&&x.GiaSuId==contact.GiaSuId&&x.BenHocId==contact.BenHocId))
            db.Conversations.Add(new() {Id=Guid.NewGuid(),LoaiHoiThoai=0,GiaSuId=contact.GiaSuId,BenHocId=contact.BenHocId,TaoLuc=DateTimeOffset.UtcNow});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }

    public async Task<List<ContactItem>> LayDanhSachAsync(Guid actor) => await (
        from c in db.ContactRequests.AsNoTracking() join u in db.Users on (c.GiaSuId==actor?c.BenHocId:c.GiaSuId) equals u.Id
        where c.GiaSuId==actor||c.BenHocId==actor orderby c.GuiLuc descending
        select new ContactItem(c,u.HoTen,c.NguoiGuiId==actor)).Take(100).ToListAsync();
}
