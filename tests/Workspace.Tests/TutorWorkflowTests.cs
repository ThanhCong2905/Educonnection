using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.Services;
using KetNoiGiaSu.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace KetNoiGiaSu.WorkspaceTests;

public sealed class TutorWorkflowTests(WorkspaceDatabase fixture) : IClassFixture<WorkspaceDatabase>
{
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName {get;set;}="Tests";
        public string EnvironmentName {get;set;}="Development";
        public string ContentRootPath {get;set;}=Path.GetTempPath();
        public string WebRootPath {get;set;}=Path.GetTempPath();
        public IFileProvider ContentRootFileProvider {get;set;}=new NullFileProvider();
        public IFileProvider WebRootFileProvider {get;set;}=new NullFileProvider();
    }
    private static QuyenTuongTacService Guard(AppDbContext db)=>new(db,new ConfigurationBuilder().Build(),new TestEnvironment());
    private static KiemDuyetHoSoService Review(AppDbContext db)=>new(db,new ProfileDataService(db,new TestEnvironment()));
    private static async Task<Guid> User(AppDbContext db,string role)
    {
        var user=new ApplicationUser {Id=Guid.NewGuid(),HoTen="Người thử "+role,Email=Guid.NewGuid()+"@example.invalid",NormalizedEmail=Guid.NewGuid().ToString(),TrangThai=0};
        db.Users.Add(user);await db.SaveChangesAsync();await TutorTestData.Role(db,user.Id,role);return user.Id;
    }
    private async Task<(Guid Tutor,Guid Learner,Guid Admin,TutorProfile Profile)> Pair(AppDbContext db)
    {
        var tutor=await User(db,"GiaSu");var learner=await User(db,"PhuHuynh");var admin=await User(db,"QuanTriVien");
        var p=new TutorProfile {Id=Guid.NewGuid(),GiaSuId=tutor,TrinhDo="Cử nhân",ChuyenMon="Toán",KinhNghiem="Kinh nghiệm đã duyệt",MoTa=Guid.NewGuid().ToString("N"),KhuVuc="",DonViHocPhi=1,TaoLuc=DateTimeOffset.UtcNow};
        db.TutorProfiles.Add(p);await db.SaveChangesAsync();await TutorTestData.Complete(db,p,fixture.SubjectId,admin);
        return(tutor,learner,admin,p);
    }
    private static TutorProfileForm Form(TutorProfile p)=>new() {Version=Convert.ToBase64String(p.RowVersion),DisplayName="Tên bản sửa",School="Trường bản sửa",Major="Sư phạm Toán",StudyStatus="DaTotNghiep",Qualification="Thạc sĩ",Expertise="Chuyên môn bản sửa",Experience="Kinh nghiệm bản sửa",Introduction="Giới thiệu bản sửa",TeachingScope="Lớp 9"};

    [Fact]
    public async Task ContactAcceptanceIsRequiredAndOnlyRecipientCanAccept()
    {
        await using var db=fixture.Open();var pair=await Pair(db);var stranger=await User(db,"HocSinh");
        var chat=new TinNhanService(db,Guard(db));var connection=new KetNoiService(db,Guard(db));var contacts=new LienHeService(db,Guard(db));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>chat.MoHoiThoaiAsync(pair.Learner,pair.Tutor));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>connection.GuiYeuCauAsync(pair.Learner,new(){Target=pair.Tutor,Note="Kết nối"}));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>contacts.GuiAsync(pair.Learner,new(){Target=pair.Tutor,Note=" "}));
        var id=await contacts.GuiAsync(pair.Learner,new(){Target=pair.Tutor,Note="Xin liên hệ để học Toán"});
        Assert.Equal(id,await contacts.GuiAsync(pair.Tutor,new(){Target=pair.Learner,Note="Gửi chéo không được tự chấp nhận"}));
        var request=await db.ContactRequests.SingleAsync(x=>x.Id==id);var version=Convert.ToBase64String(request.RowVersion);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>contacts.PhanHoiAsync(pair.Learner,id,"accept",version));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>contacts.PhanHoiAsync(stranger,id,"accept",version));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>chat.MoHoiThoaiAsync(pair.Learner,pair.Tutor));
        await contacts.PhanHoiAsync(pair.Tutor,id,"accept",version);
        var conversation=await chat.MoHoiThoaiAsync(pair.Learner,pair.Tutor);
        await chat.GuiAsync(pair.Learner,new(){ConversationId=conversation,OperationId=Guid.NewGuid(),Text="Xin chào"});
        Assert.Single(await chat.LayDanhSachAsync(pair.Tutor,conversation));
        Assert.NotEqual(Guid.Empty,await connection.GuiYeuCauAsync(pair.Tutor,new(){Target=pair.Learner,Note="Mời kết nối lớp"}));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>contacts.PhanHoiAsync(pair.Tutor,id,"accept",version));
        Assert.Single(await db.Conversations.Where(x=>x.GiaSuId==pair.Tutor&&x.BenHocId==pair.Learner).ToListAsync());
    }

    [Fact]
    public async Task RejectionAndWithdrawalDoNotGrantMessagingPermission()
    {
        await using var db=fixture.Open();var pair=await Pair(db);var contacts=new LienHeService(db,Guard(db));var chat=new TinNhanService(db,Guard(db));
        var id=await contacts.GuiAsync(pair.Tutor,new(){Target=pair.Learner,Note="Gia sư xin liên hệ"});
        var r=await db.ContactRequests.SingleAsync(x=>x.Id==id);
        await contacts.PhanHoiAsync(pair.Learner,id,"reject",Convert.ToBase64String(r.RowVersion));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>chat.MoHoiThoaiAsync(pair.Learner,pair.Tutor));
        Assert.Equal(id,await contacts.GuiAsync(pair.Learner,new(){Target=pair.Tutor,Note="Xin liên hệ lại"}));
        await db.Entry(r).ReloadAsync();Assert.Equal(pair.Learner,r.NguoiGuiId);Assert.Equal(ContactState.Pending,r.TrangThai);
        await contacts.PhanHoiAsync(pair.Learner,id,"withdraw",Convert.ToBase64String(r.RowVersion));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>chat.MoHoiThoaiAsync(pair.Learner,pair.Tutor));
    }

    [Fact]
    public async Task LegacyConversationCannotBypassContactGate()
    {
        await using var db=fixture.Open();var pair=await Pair(db);
        var legacy=new Conversation {Id=Guid.NewGuid(),GiaSuId=pair.Tutor,BenHocId=pair.Learner,TaoLuc=DateTimeOffset.UtcNow};db.Conversations.Add(legacy);await db.SaveChangesAsync();
        var chat=new TinNhanService(db,Guard(db));Assert.Null(await chat.LayHoiThoaiAsync(pair.Learner,legacy.Id));
        Assert.Empty(await chat.LayHoiThoaiTheoTaiKhoanAsync(pair.Learner));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>chat.GuiAsync(pair.Learner,new(){ConversationId=legacy.Id,OperationId=Guid.NewGuid(),Text="Bỏ qua liên hệ"}));
        var contacts=new LienHeService(db,Guard(db));var id=await contacts.GuiAsync(pair.Learner,new(){Target=pair.Tutor,Note="Liên hệ"});var r=await db.ContactRequests.SingleAsync(x=>x.Id==id);
        await contacts.PhanHoiAsync(pair.Tutor,id,"accept",Convert.ToBase64String(r.RowVersion));
        Assert.Equal(legacy.Id,await chat.MoHoiThoaiAsync(pair.Tutor,pair.Learner));
    }

    [Fact]
    public async Task ConditionsPublishImmediatelyWithoutChangingReviewedProfileOrDraft()
    {
        await using var db=fixture.Open();var pair=await Pair(db);var professional=new HoSoGiaSuService(db);
        var edit=Form(pair.Profile);edit.Subjects=[fixture.SubjectId];await professional.CapNhatAsync(pair.Tutor,edit);
        var revision=await db.TutorProfileRevisions.SingleAsync(x=>x.HoSoGiaSuId==pair.Profile.Id);var version=revision.RowVersion.ToArray();
        var service=new DieuKienNhanLopService(db);var form=await service.LayDeSuaAsync(pair.Tutor);form.Fee=350000;form.Online=true;form.InPerson=true;form.Region="Hà Nội";form.Slots=[new(){Enabled=true,Day=3,Start=new(17,0),End=new(19,0)}];
        await service.CapNhatAsync(pair.Tutor,form);db.ChangeTracker.Clear();
        var profile=await db.TutorProfiles.SingleAsync(x=>x.Id==pair.Profile.Id);Assert.Equal(ProfileReviewState.Approved,profile.TrangThaiDuyet);Assert.Equal("Kinh nghiệm đã duyệt",profile.KinhNghiem);
        Assert.Equal(version,(await db.TutorProfileRevisions.SingleAsync(x=>x.Id==revision.Id)).RowVersion);
        var condition=await db.TutorConditions.SingleAsync(x=>x.HoSoGiaSuId==profile.Id);Assert.Equal(350000,condition.HocPhi);Assert.True(condition.DayTrucTiep);
        Assert.Equal(3,(await db.TutorAvailability.SingleAsync(x=>x.HoSoGiaSuId==profile.Id)).ThuTrongTuan);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CapNhatAsync(pair.Tutor,form));
    }

    [Fact]
    public async Task InvalidOverlappingScheduleDoesNotPartiallySaveFee()
    {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new DieuKienNhanLopService(db);var form=await service.LayDeSuaAsync(pair.Tutor);
        form.Fee=999000;form.Slots=[new(){Enabled=true,Day=1,Start=new(18,0),End=new(20,0)},new(){Enabled=true,Day=1,Start=new(19,0),End=new(21,0)}];
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CapNhatAsync(pair.Tutor,form));
        Assert.Equal(200000,(await db.TutorConditions.AsNoTracking().SingleAsync(x=>x.HoSoGiaSuId==pair.Profile.Id)).HocPhi);
        form.Slots=[];form.AcceptingStudents=true;await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CapNhatAsync(pair.Tutor,form));
        form.AcceptingStudents=false;form.Fee=null;await service.CapNhatAsync(pair.Tutor,form);
        Assert.Null((await db.TutorConditions.SingleAsync(x=>x.HoSoGiaSuId==pair.Profile.Id)).DonViHocPhi);
    }

    [Fact]
    public async Task OnlyApprovedRevisionReplacesPublicFieldsAndRetainsCurrentConditions()
    {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new HoSoGiaSuService(db);var form=Form(pair.Profile);form.Subjects=[fixture.SubjectId];
        await service.CapNhatAsync(pair.Tutor,form);db.ChangeTracker.Clear();
        var detail=await service.LayHoSoAsync(pair.Tutor,new(){UserId=pair.Learner},null);Assert.NotNull(detail);Assert.Equal("Kinh nghiệm đã duyệt",detail.Profile.KinhNghiem);Assert.Null(detail.LatestRevision);
        var revision=await db.TutorProfileRevisions.SingleAsync(x=>x.HoSoGiaSuId==pair.Profile.Id);var version=Convert.ToBase64String(revision.RowVersion);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Review(db).DuyetAsync(pair.Learner,revision.Id,true,version,null));
        await Review(db).DuyetAsync(pair.Admin,revision.Id,true,version,null);db.ChangeTracker.Clear();
        var approved=await db.TutorProfiles.SingleAsync(x=>x.Id==pair.Profile.Id);Assert.Equal("Tên bản sửa",approved.TenHienThi);Assert.Equal("Kinh nghiệm bản sửa",approved.KinhNghiem);Assert.Equal(pair.Admin,approved.NguoiDuyetId);
        Assert.Equal(200000,(await db.TutorConditions.SingleAsync(x=>x.HoSoGiaSuId==approved.Id)).HocPhi);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Review(db).DuyetAsync(pair.Admin,revision.Id,true,version,null));
        var evidence=await db.ProfileAttachments.SingleAsync(x=>x.HoSoGiaSuId==approved.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>new ProfileDataService(db,new TestEnvironment()).DeleteAsync(pair.Tutor,evidence.Id));
    }

    [Fact]
    public async Task RejectingRevisionKeepsPublishedProfileAndCannotBeSelfReviewed()
    {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new HoSoGiaSuService(db);var form=Form(pair.Profile);form.Subjects=[fixture.SubjectId];await service.CapNhatAsync(pair.Tutor,form);
        var r=await db.TutorProfileRevisions.SingleAsync(x=>x.HoSoGiaSuId==pair.Profile.Id);var version=Convert.ToBase64String(r.RowVersion);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Review(db).DuyetAsync(pair.Tutor,r.Id,true,version,null));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Review(db).DuyetAsync(pair.Admin,r.Id,false,version," "));
        await Review(db).DuyetAsync(pair.Admin,r.Id,false,version,"Minh chứng chưa phù hợp");db.ChangeTracker.Clear();
        var p=await db.TutorProfiles.SingleAsync(x=>x.Id==pair.Profile.Id);Assert.Equal(ProfileReviewState.Approved,p.TrangThaiDuyet);Assert.Equal("Kinh nghiệm đã duyệt",p.KinhNghiem);
        Assert.Equal(ProfileReviewState.Rejected,(await db.TutorProfileRevisions.SingleAsync(x=>x.Id==r.Id)).TrangThai);
    }

    [Fact]
    public async Task PendingRevisionUsesItsOwnVersionAndCannotOverwriteNewerSubmission()
    {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new HoSoGiaSuService(db);var form=Form(pair.Profile);form.Subjects=[fixture.SubjectId];await service.CapNhatAsync(pair.Tutor,form);
        var workspace=await new WorkspaceService(db).GetWorkspaceAsync(pair.Tutor,true,false);var current=await service.LayDeSuaAsync(workspace);var stale=await service.LayDeSuaAsync(workspace);
        current.Experience="Thay đổi mới nhất";await service.CapNhatAsync(pair.Tutor,current);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CapNhatAsync(pair.Tutor,stale));
        Assert.Equal("Thay đổi mới nhất",(await db.TutorProfileRevisions.AsNoTracking().SingleAsync(x=>x.HoSoGiaSuId==pair.Profile.Id)).KinhNghiem);
    }

    [Fact]
    public async Task SearchUsesProfileWithoutTeachingNeedAndFiltersLiveConditions()
    {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new TimKiemService(new NhuCauService(db),db);
        Assert.False(await db.Needs.AnyAsync(x=>x.ChuSoHuuId==pair.Tutor));
        var workspace=new WorkspaceViewModel {Search=new(){Q=pair.Profile.MoTa,SubjectId=fixture.SubjectId,Grade="Lớp 9",Day=1,From=new(18,30),To=new(19,30),Format=0,MaxFee=250000,FeeUnit=1}};
        await service.TimKiemAsync(workspace);Assert.Single(workspace.Cards);Assert.Equal(pair.Profile.Id,workspace.Cards[0].Id);
        workspace.Search.Day=2;await service.TimKiemAsync(workspace);Assert.Empty(workspace.Cards);
        workspace.Search.Day=1;workspace.Search.Grade="Lớp 1";await service.TimKiemAsync(workspace);Assert.Empty(workspace.Cards);
        workspace.Search.Grade="Lớp 9";var conditions=new DieuKienNhanLopService(db);var edit=await conditions.LayDeSuaAsync(pair.Tutor);edit.Fee=400000;await conditions.CapNhatAsync(pair.Tutor,edit);
        await service.TimKiemAsync(workspace);Assert.Empty(workspace.Cards);
    }

    [Fact]
    public async Task TutorCannotCreateTeachingNeedThroughForgedWorkspace()
    {
        await using var db=fixture.Open();var pair=await Pair(db);var service=new NhuCauService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CapNhatAsync(pair.Tutor,new(){IsTutor=false,HasRequiredAddress=true},new(){SubjectId=fixture.SubjectId,Grade="Lớp 9",Goal="Nhu cầu dạy"}));
        Assert.False(await db.Needs.AnyAsync(x=>x.ChuSoHuuId==pair.Tutor));
    }
}
