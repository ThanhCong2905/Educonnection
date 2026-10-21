using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class TinNhanService(AppDbContext db, QuyenTuongTacService quyen)
{
    public async Task<Guid> MoHoiThoaiAsync(Guid actor,Guid target)
    {
        await using var transaction=await db.Database.BeginTransactionAsync();var pair=await quyen.PairAsync(actor,target);await quyen.LockAsync($"chat:{pair.Tutor}:{pair.Learner}");
        await quyen.RequireAcceptedContactAsync(pair.Tutor,pair.Learner);
        var chat=await db.Conversations.SingleOrDefaultAsync(x=>x.LoaiHoiThoai==0 && x.GiaSuId==pair.Tutor && x.BenHocId==pair.Learner);
        if(chat==null) {chat=new(){Id=Guid.NewGuid(),GiaSuId=pair.Tutor,BenHocId=pair.Learner,TaoLuc=DateTimeOffset.UtcNow};db.Conversations.Add(chat);await db.SaveChangesAsync();}
        await transaction.CommitAsync();return chat.Id;
    }

    public Task<Conversation?> LayHoiThoaiAsync(Guid actor,Guid id) => db.Conversations.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id && x.LoaiHoiThoai==0 && (x.GiaSuId==actor || x.BenHocId==actor) && db.ContactRequests.Any(c=>c.GiaSuId==x.GiaSuId&&c.BenHocId==x.BenHocId&&c.TrangThai==ContactState.Accepted));

    public async Task<List<ConversationItem>> LayHoiThoaiTheoTaiKhoanAsync(Guid actor) => await (from c in db.Conversations.AsNoTracking() join u in db.Users on (c.GiaSuId==actor?c.BenHocId:c.GiaSuId) equals (Guid?)u.Id
        where c.LoaiHoiThoai==0 && (c.GiaSuId==actor || c.BenHocId==actor)
        && db.ContactRequests.Any(r=>r.GiaSuId==c.GiaSuId&&r.BenHocId==c.BenHocId&&r.TrangThai==ContactState.Accepted)
        let last=db.ChatMessages.Where(m=>m.CuocHoiThoaiId==c.Id).OrderByDescending(m=>m.GuiLuc).ThenByDescending(m=>m.Id).FirstOrDefault()
        orderby (last==null?c.TaoLuc:last.GuiLuc) descending
        select new ConversationItem(c.Id,u.HoTen,last==null?null:last.NoiDung,c.TaoLuc)).Take(100).ToListAsync();

    public async Task<List<ChatMessage>> LayDanhSachAsync(Guid actor,Guid id,int page=1)
    {
        if(await LayHoiThoaiAsync(actor,id)==null) throw new InvalidOperationException("Bạn không có quyền xem cuộc trò chuyện.");
        var result=await db.ChatMessages.AsNoTracking().Where(x=>x.CuocHoiThoaiId==id).OrderByDescending(x=>x.GuiLuc).ThenByDescending(x=>x.Id).Skip((Math.Clamp(page,1,10000)-1)*100).Take(100).ToListAsync();result.Reverse();return result;
    }

    public async Task<Guid> GuiAsync(Guid actor,SendMessageForm form)
    {
        if(string.IsNullOrWhiteSpace(form.Text)||form.Text.Length>4000||form.OperationId==Guid.Empty) throw new InvalidOperationException("Tin nhắn phải có từ 1 đến 4000 ký tự.");
        await using var transaction=await db.Database.BeginTransactionAsync();
        var chat=await LayHoiThoaiAsync(actor,form.ConversationId) ?? throw new InvalidOperationException("Bạn không có quyền gửi tin nhắn.");
        var pair=await quyen.PairAsync(actor,(chat.GiaSuId==actor?chat.BenHocId:chat.GiaSuId)!.Value);
        await quyen.RequireAcceptedContactAsync(pair.Tutor,pair.Learner);await quyen.LockAsync($"message:{actor}:{form.OperationId}");
        var existing=await db.ChatMessages.SingleOrDefaultAsync(x=>x.NguoiGuiId==actor && x.MaThaoTac==form.OperationId);
        if(existing!=null) {if(existing.CuocHoiThoaiId!=form.ConversationId || existing.NoiDung!=form.Text.Trim())throw new InvalidOperationException("Mã gửi tin đã được sử dụng.");await transaction.CommitAsync();return existing.Id;}
        var message=new ChatMessage {Id=Guid.NewGuid(),CuocHoiThoaiId=chat.Id,NguoiGuiId=actor,NoiDung=form.Text.Trim(),GuiLuc=DateTimeOffset.UtcNow,MaThaoTac=form.OperationId};
        db.ChatMessages.Add(message);await db.SaveChangesAsync();await transaction.CommitAsync();return message.Id;
    }

}
