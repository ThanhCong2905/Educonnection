using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.Services;
using KetNoiGiaSu.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;
namespace KetNoiGiaSu.WorkspaceTests;

public sealed class CommunicationTests(WorkspaceDatabase fixture) : IClassFixture<WorkspaceDatabase>
{
    private sealed class TestEnvironment : IWebHostEnvironment {
        public string ApplicationName {get;set;}="Tests";public string EnvironmentName {get;set;}="Development";
        public string ContentRootPath {get;set;}="";public string WebRootPath {get;set;}="";
        public IFileProvider ContentRootFileProvider {get;set;}=new NullFileProvider();public IFileProvider WebRootFileProvider {get;set;}=new NullFileProvider();
    }
    private static QuyenTuongTacService Quyen(AppDbContext db,bool bypass=true,string environment="Development") => new(db,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Workspace:BypassTutorApproval"]=bypass.ToString() }).Build(),new TestEnvironment {EnvironmentName=environment});
    private static async Task<Guid> User(AppDbContext db,string role) {
        var user=new ApplicationUser {Id=Guid.NewGuid(),HoTen="Người kiểm thử "+role,Email=Guid.NewGuid()+"@example.invalid",NormalizedEmail=Guid.NewGuid().ToString(),TrangThai=0};
        db.Users.Add(user);await db.SaveChangesAsync();
        var identityRole=await db.Roles.SingleOrDefaultAsync(x=>x.Name==role);
        if(identityRole==null){identityRole=new IdentityRole<Guid> {Id=Guid.NewGuid(),Name=role,NormalizedName=role.ToUpperInvariant()};db.Roles.Add(identityRole);await db.SaveChangesAsync();}
        db.UserRoles.Add(new(){UserId=user.Id,RoleId=identityRole.Id});
        db.PersonalProfiles.Add(new(){TaiKhoanId=user.Id,ThanhPho="Thành phố",DiaChiChiTiet="Địa chỉ riêng"});await db.SaveChangesAsync();return user.Id;
    }
    private static async Task<(Guid Tutor,Guid Learner)> Pair(AppDbContext db) {
        var tutor=await User(db,"GiaSu");var learner=await User(db,"PhuHuynh");
        var profile=new TutorProfile {Id=Guid.NewGuid(),GiaSuId=tutor,TrinhDo="Cử nhân",ChuyenMon="Toán",KinhNghiem="Giảng dạy",MoTa="Giới thiệu",KhuVuc="Thành phố",TaoLuc=DateTimeOffset.UtcNow};
        db.TutorProfiles.Add(profile);await db.SaveChangesAsync();
        var reviewer=await User(db,"QuanTriVien");
        await TutorTestData.Complete(db,profile,await db.Subjects.Select(x=>x.Id).FirstAsync(),reviewer);
        var contacts=new LienHeService(db,Quyen(db));var contactId=await contacts.GuiAsync(learner,new(){Target=tutor,Note="Xin liên hệ"});
        var contact=await db.ContactRequests.SingleAsync(x=>x.Id==contactId);
        await contacts.PhanHoiAsync(tutor,contactId,"accept",Convert.ToBase64String(contact.RowVersion));
        return(tutor,learner);
    }
    private ClassProposalForm Proposal(ConnectionRequest r) => new(){Id=r.Id,Version=Convert.ToBase64String(r.RowVersion),Name="Lớp Toán",SubjectId=fixture.SubjectId,Goal="Củng cố kiến thức",Schedule="Thứ 2, 18:00; bắt đầu tuần tới",Duration=90,Sessions=2,Fee=200000,FeeUnit=1};
    [Fact]
    public async Task ProposalAcceptedByLearnerCreatesExactlyOneClass() {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new KetNoiService(db,Quyen(db));
        var id=await service.GuiYeuCauAsync(pair.Learner,new(){Target=pair.Tutor,Note="Tôi muốn tìm gia sư"});
        Assert.Equal(id,await service.GuiYeuCauAsync(pair.Learner,new(){Target=pair.Tutor,Note="Gửi lại"}));
        Assert.Empty(await new LopHocService(db).LayDanhSachAsync(pair.Learner));var request=(await service.LayYeuCauAsync(pair.Tutor,id))!;
        await service.GuiDeXuatAsync(pair.Tutor,Proposal(request));request=(await service.LayYeuCauAsync(pair.Learner,id))!;
        Assert.Equal(ConnectionState.Proposed,request.TrangThai);var version=Convert.ToBase64String(request.RowVersion);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.PhanHoiAsync(pair.Tutor,id,"accept",version));
        await service.PhanHoiAsync(pair.Learner,id,"accept",version);
        Assert.Single(await new LopHocService(db).LayDanhSachAsync(pair.Learner));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.PhanHoiAsync(pair.Learner,id,"accept",version));
        Assert.Single(await new LopHocService(db).LayDanhSachAsync(pair.Tutor));
    }
    [Fact]
    public async Task ChatIsPrivateAndRetriesDoNotDuplicateMessages() {
        await using var db=fixture.Open();var pair=await Pair(db);var stranger=await User(db,"HocSinh");var service=new TinNhanService(db,Quyen(db));
        var chat=await service.MoHoiThoaiAsync(pair.Learner,pair.Tutor);Assert.Equal(chat,await service.MoHoiThoaiAsync(pair.Tutor,pair.Learner));
        var form=new SendMessageForm {ConversationId=chat,OperationId=Guid.NewGuid(),Text="Xin chào <script>alert(1)</script>"};
        var message=await service.GuiAsync(pair.Learner,form);Assert.Equal(message,await service.GuiAsync(pair.Learner,form));
        Assert.Single(await service.LayDanhSachAsync(pair.Tutor,chat));Assert.Single(await service.LayHoiThoaiTheoTaiKhoanAsync(pair.Learner));
        Assert.Null(await service.LayHoiThoaiAsync(stranger,chat));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.LayDanhSachAsync(stranger,chat));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.GuiAsync(stranger,new(){ConversationId=chat,OperationId=Guid.NewGuid(),Text="Xâm nhập"}));
    }
    [Fact]
    public async Task ApprovalBypassOnlyWorksInDevelopmentAndStillRequiresCompleteProfile() {
        await using var db=fixture.Open();var pair=await Pair(db);
        var profile=await db.TutorProfiles.SingleAsync(x=>x.GiaSuId==pair.Tutor);profile.TrangThaiDuyet=ProfileReviewState.Pending;profile.NguoiDuyetId=null;profile.DuyetLuc=null;await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>new TinNhanService(db,Quyen(db,false)).MoHoiThoaiAsync(pair.Tutor,pair.Learner));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>new KetNoiService(db,Quyen(db,true,"Production")).GuiYeuCauAsync(pair.Learner,new(){Target=pair.Tutor,Note="Thử"}));
        Assert.NotEqual(Guid.Empty,await new TinNhanService(db,Quyen(db)).MoHoiThoaiAsync(pair.Tutor,pair.Learner));
        await db.ProfileAttachments.Where(x=>x.HoSoGiaSuId==profile.Id).ExecuteDeleteAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>new TinNhanService(db,Quyen(db)).MoHoiThoaiAsync(pair.Tutor,pair.Learner));
    }
    [Fact]
    public async Task ExpiredRequestsCannotBeAcceptedAndCanBeSentAgain() {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new KetNoiService(db,Quyen(db));
        var id=await service.GuiYeuCauAsync(pair.Learner,new(){Target=pair.Tutor,Note="Kết nối"});
        await service.GuiDeXuatAsync(pair.Tutor,Proposal((await service.LayYeuCauAsync(pair.Tutor,id))!));
        var request=await db.ConnectionRequests.SingleAsync(x=>x.Id==id);request.GuiLuc=DateTimeOffset.UtcNow.AddHours(-74);request.HetHanLuc=DateTimeOffset.UtcNow.AddHours(-2);await db.SaveChangesAsync();
        await service.XuLyHetHanYeuCauAsync();db.ChangeTracker.Clear();request=(await service.LayYeuCauAsync(pair.Learner,id))!;Assert.Equal(ConnectionState.Expired,request.TrangThai);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.PhanHoiAsync(pair.Learner,id,"accept",Convert.ToBase64String(request.RowVersion)));
        Assert.NotEqual(id,await service.GuiYeuCauAsync(pair.Learner,new(){Target=pair.Tutor,Note="Kết nối lại"}));
    }
    [Fact]
    public async Task SelfSameRoleAndUnrelatedNeedAreRejected() {
        await using var db=fixture.Open();var pair=await Pair(db);var stranger=await User(db,"HocSinh");var service=new TinNhanService(db,Quyen(db));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.MoHoiThoaiAsync(pair.Tutor,pair.Tutor));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.MoHoiThoaiAsync(pair.Learner,stranger));
        var need=new LearningNeed {Id=Guid.NewGuid(),ChuSoHuuId=stranger,MonHocId=fixture.SubjectId,TrangThai=NeedState.Open,TaoLuc=DateTimeOffset.UtcNow};db.Needs.Add(need);await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>new KetNoiService(db,Quyen(db)).GuiYeuCauAsync(pair.Learner,new(){Target=pair.Tutor,NeedId=need.Id,Note="Thử"}));
    }
    [Fact]
    public async Task LockedAccountAndMissingCertificateCannotSend() {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new TinNhanService(db,Quyen(db));var chat=await service.MoHoiThoaiAsync(pair.Tutor,pair.Learner);
        var account=await db.Users.SingleAsync(x=>x.Id==pair.Tutor);account.TrangThai=1;await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.GuiAsync(pair.Learner,new(){ConversationId=chat,OperationId=Guid.NewGuid(),Text="Thử"}));
        account.TrangThai=0;await db.SaveChangesAsync();await db.ProfileAttachments.Where(x=>x.NguoiTaiLenId==pair.Tutor).ExecuteDeleteAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.MoHoiThoaiAsync(pair.Tutor,pair.Learner));
    }
    [Fact]
    public async Task OnlyCorrectPartyCanRejectOrWithdraw() {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new KetNoiService(db,Quyen(db));var id=await service.GuiYeuCauAsync(pair.Learner,new(){Target=pair.Tutor,Note="Thử"});
        var r=(await service.LayYeuCauAsync(pair.Tutor,id))!;var version=Convert.ToBase64String(r.RowVersion);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.PhanHoiAsync(pair.Tutor,id,"cancel",version));
        await service.PhanHoiAsync(pair.Tutor,id,"reject",version);Assert.Equal(ConnectionState.Rejected,(await service.LayYeuCauAsync(pair.Learner,id))!.TrangThai);
    }
}
