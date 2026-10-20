using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class CommunicationService(AppDbContext db, IConfiguration configuration, IWebHostEnvironment environment)
{
    private bool Bypass => environment.IsDevelopment() && configuration.GetValue<bool>("Workspace:BypassTutorApproval");
    private async Task LockAsync(string resource) => await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result=sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000; IF @result<0 THROW 50001, 'Please retry this operation.', 1;");
    private async Task<string[]> RolesAsync(Guid id) => await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==id select r.Name!).ToArrayAsync();
    private async Task<(Guid Tutor,Guid Learner)> PairAsync(Guid actor,Guid target)
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
    public Task ExpireAsync() => db.ConnectionRequests.Where(x=>(x.TrangThai==ConnectionState.Requested || x.TrangThai==ConnectionState.Proposed) && x.HetHanLuc<=DateTimeOffset.UtcNow)
        .ExecuteUpdateAsync(set=>set.SetProperty(x=>x.TrangThai,ConnectionState.Expired));

    public async Task<Guid> ConnectAsync(Guid actor,ConnectForm form)
    {
        if(string.IsNullOrWhiteSpace(form.Note) || form.Note.Length>2000) throw new InvalidOperationException("Lời nhắn phải có từ 1 đến 2000 ký tự.");
        await ExpireAsync(); await using var transaction=await db.Database.BeginTransactionAsync();
        var pair=await PairAsync(actor,form.Target); await LockAsync($"connection:{pair.Tutor}:{pair.Learner}");
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
    public async Task<List<ConnectionItem>> RequestsAsync(Guid actor)
    {
        await ExpireAsync();
        return await (from r in db.ConnectionRequests.AsNoTracking() join u in db.Users on (r.GiaSuId==actor?r.BenHocId:r.GiaSuId) equals u.Id
                      where r.GiaSuId==actor || r.BenHocId==actor orderby r.GuiLuc descending select new ConnectionItem(r,u.HoTen,r.NguoiGuiId==actor)).Take(100).ToListAsync();
    }
    public Task<ConnectionRequest?> RequestAsync(Guid actor,Guid id) => db.ConnectionRequests.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id && (x.GiaSuId==actor || x.BenHocId==actor));
    private static void CheckVersion(ConnectionRequest request,string version) {
        if(Convert.ToBase64String(request.RowVersion)!=version) throw new InvalidOperationException("Yêu cầu đã thay đổi. Vui lòng tải lại trang.");
    }
    public async Task ProposeAsync(Guid actor,ClassProposalForm form)
    {
        if(string.IsNullOrWhiteSpace(form.Name)||form.Name.Length>200||string.IsNullOrWhiteSpace(form.Goal)||form.Goal.Length>2000||string.IsNullOrWhiteSpace(form.Schedule)||form.Schedule.Length>2000||form.Sessions is <1 or >7||form.Duration is <1 or >480||form.Format is <0 or >1||form.Fee is <0 or >100000000||form.FeeUnit is <0 or >2||form.Location?.Length>500 || (form.Format==1&&string.IsNullOrWhiteSpace(form.Location)))
            throw new InvalidOperationException("Vui lòng nhập đầy đủ và đúng điều kiện lớp học.");
        await ExpireAsync();await using var transaction=await db.Database.BeginTransactionAsync();
        var request=await db.ConnectionRequests.SingleOrDefaultAsync(x=>x.Id==form.Id && x.GiaSuId==actor) ?? throw new InvalidOperationException("Chỉ gia sư của yêu cầu được đề xuất lớp.");
        await PairAsync(actor,request.BenHocId);await LockAsync($"request:{request.Id}");await db.Entry(request).ReloadAsync();CheckVersion(request,form.Version);
        if(request.TrangThai is not (ConnectionState.Requested or ConnectionState.Proposed) || request.HetHanLuc<=DateTimeOffset.UtcNow) throw new InvalidOperationException("Yêu cầu không còn chờ phản hồi.");
        if(!await db.Subjects.AnyAsync(x=>x.Id==form.SubjectId && x.DangSuDung)) throw new InvalidOperationException("Môn học không khả dụng.");
        request.TenLopDuKien=form.Name.Trim();request.MonHocId=form.SubjectId;request.MucTieu=form.Goal.Trim();request.SoBuoiTuan=form.Sessions;request.ThoiLuongPhut=form.Duration;
        request.LichDuKien=form.Schedule.Trim();request.HinhThucHoc=form.Format;request.DiaDiem=form.Format==1?form.Location?.Trim():null;request.HocPhi=form.Fee;request.DonViHocPhi=form.FeeUnit;
        request.TrangThai=ConnectionState.Proposed;request.GuiLuc=DateTimeOffset.UtcNow;request.HetHanLuc=request.GuiLuc.AddHours(72);
        await db.SaveChangesAsync();await transaction.CommitAsync();
    }
    public async Task RespondAsync(Guid actor,Guid id,string action,string version)
    {
        await ExpireAsync();await using var transaction=await db.Database.BeginTransactionAsync();
        var request=await db.ConnectionRequests.SingleOrDefaultAsync(x=>x.Id==id && (x.GiaSuId==actor || x.BenHocId==actor)) ?? throw new InvalidOperationException("Không tìm thấy yêu cầu của bạn.");
        await PairAsync(actor,request.GiaSuId==actor?request.BenHocId:request.GiaSuId);await LockAsync($"request:{id}");await db.Entry(request).ReloadAsync();CheckVersion(request,version);
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
    public async Task<Guid> OpenChatAsync(Guid actor,Guid target)
    {
        await using var transaction=await db.Database.BeginTransactionAsync();var pair=await PairAsync(actor,target);await LockAsync($"chat:{pair.Tutor}:{pair.Learner}");
        var chat=await db.Conversations.SingleOrDefaultAsync(x=>x.LoaiHoiThoai==0 && x.GiaSuId==pair.Tutor && x.BenHocId==pair.Learner);
        if(chat==null) {chat=new(){Id=Guid.NewGuid(),GiaSuId=pair.Tutor,BenHocId=pair.Learner,TaoLuc=DateTimeOffset.UtcNow};db.Conversations.Add(chat);await db.SaveChangesAsync();}
        await transaction.CommitAsync();return chat.Id;
    }
    public Task<Conversation?> ConversationAsync(Guid actor,Guid id) => db.Conversations.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id && x.LoaiHoiThoai==0 && (x.GiaSuId==actor || x.BenHocId==actor));
    public async Task<List<ConversationItem>> ChatsAsync(Guid actor) => await (from c in db.Conversations.AsNoTracking() join u in db.Users on (c.GiaSuId==actor?c.BenHocId:c.GiaSuId) equals (Guid?)u.Id
        where c.LoaiHoiThoai==0 && (c.GiaSuId==actor || c.BenHocId==actor)
        let last=db.ChatMessages.Where(m=>m.CuocHoiThoaiId==c.Id).OrderByDescending(m=>m.GuiLuc).ThenByDescending(m=>m.Id).FirstOrDefault()
        orderby (last==null?c.TaoLuc:last.GuiLuc) descending
        select new ConversationItem(c.Id,u.HoTen,last==null?null:last.NoiDung,c.TaoLuc)).Take(100).ToListAsync();
    public async Task<List<ChatMessage>> MessagesAsync(Guid actor,Guid id,int page=1)
    {
        if(await ConversationAsync(actor,id)==null) throw new InvalidOperationException("Bạn không có quyền xem cuộc trò chuyện.");
        var result=await db.ChatMessages.AsNoTracking().Where(x=>x.CuocHoiThoaiId==id).OrderByDescending(x=>x.GuiLuc).ThenByDescending(x=>x.Id).Skip((Math.Clamp(page,1,10000)-1)*100).Take(100).ToListAsync();result.Reverse();return result;
    }
    public async Task<Guid> SendAsync(Guid actor,SendMessageForm form)
    {
        if(string.IsNullOrWhiteSpace(form.Text)||form.Text.Length>4000||form.OperationId==Guid.Empty) throw new InvalidOperationException("Tin nhắn phải có từ 1 đến 4000 ký tự.");
        await using var transaction=await db.Database.BeginTransactionAsync();
        var chat=await ConversationAsync(actor,form.ConversationId) ?? throw new InvalidOperationException("Bạn không có quyền gửi tin nhắn.");
        await PairAsync(actor,(chat.GiaSuId==actor?chat.BenHocId:chat.GiaSuId)!.Value);await LockAsync($"message:{actor}:{form.OperationId}");
        var existing=await db.ChatMessages.SingleOrDefaultAsync(x=>x.NguoiGuiId==actor && x.MaThaoTac==form.OperationId);
        if(existing!=null) {if(existing.CuocHoiThoaiId!=form.ConversationId || existing.NoiDung!=form.Text.Trim())throw new InvalidOperationException("Mã gửi tin đã được sử dụng.");await transaction.CommitAsync();return existing.Id;}
        var message=new ChatMessage {Id=Guid.NewGuid(),CuocHoiThoaiId=chat.Id,NguoiGuiId=actor,NoiDung=form.Text.Trim(),GuiLuc=DateTimeOffset.UtcNow,MaThaoTac=form.OperationId};
        db.ChatMessages.Add(message);await db.SaveChangesAsync();await transaction.CommitAsync();return message.Id;
    }
    public Task<List<ConnectedClass>> ClassesAsync(Guid actor) => db.ConnectedClasses.AsNoTracking().Where(x=>x.GiaSuId==actor || x.BenHocId==actor).OrderByDescending(x=>x.TaoLuc).Take(100).ToListAsync();
}

public sealed class ConnectionExpiryWorker(IServiceScopeFactory scopes,ILogger<ConnectionExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var timer=new PeriodicTimer(TimeSpan.FromMinutes(1));
        while(!stoppingToken.IsCancellationRequested) {
            try {using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<CommunicationService>().ExpireAsync();}
            catch(Exception ex) when(!stoppingToken.IsCancellationRequested) {logger.LogWarning(ex,"Unable to expire connection requests.");}
            try {if(!await timer.WaitForNextTickAsync(stoppingToken))break;}catch(OperationCanceledException){break;}
        }
    }
}
