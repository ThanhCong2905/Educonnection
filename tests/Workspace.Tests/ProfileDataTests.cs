using KetNoiGiaSu.Models;
using KetNoiGiaSu.Services;
using KetNoiGiaSu.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace KetNoiGiaSu.WorkspaceTests;

public sealed class ProfileDataTests(WorkspaceDatabase fixture) : IClassFixture<WorkspaceDatabase>
{
    private sealed class TestEnvironment : IWebHostEnvironment {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(),"edu-profile-tests-"+Guid.NewGuid().ToString("N"));
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
    private static ApplicationUser User() => new() { Id=Guid.NewGuid(),HoTen="Kiểm thử",Email=Guid.NewGuid()+"@example.invalid",NormalizedEmail=Guid.NewGuid().ToString(),TrangThai=0 };

    [Fact]
    public async Task AddressIsOwnedAndExistingNeedKeepsItsSnapshot() {
        await using var db=fixture.Open(); var user=User(); var other=User(); db.Users.AddRange(user,other); await db.SaveChangesAsync();
        var service=new ProfileDataService(db,new TestEnvironment());
        await service.SaveAddressAsync(user.Id,new() {City="Thành phố thử",District="Khu vực thử",DetailedAddress="Địa chỉ riêng"});
        Assert.Null(await service.AddressAsync(other.Id));
        var address=(await service.AddressAsync(user.Id))!;
        var workspaceService=new WorkspaceService(db); var workspace=await workspaceService.GetWorkspaceAsync(user.Id,false,false);
        await workspaceService.SaveAsync(user.Id,workspace,new NeedForm {SubjectId=fixture.SubjectId,Grade="Lớp 9",Goal="Ôn tập",Format=1,Region=address.PublicRegion,DetailedAddress=address.DiaChiChiTiet,Fee=100000,Publish=true,Days=[1]});
        await service.SaveAddressAsync(user.Id,new() {City="Thành phố mới",DetailedAddress="Địa chỉ mới"});
        var cards=await workspaceService.MyCardsAsync(user.Id); var need=await workspaceService.OwnedNeedAsync(cards.Single().Id,user.Id);
        Assert.Equal("Địa chỉ riêng",need!.DiaChiChiTiet); Assert.Equal("Khu vực thử, Thành phố thử",cards.Single().Region);
        Assert.DoesNotContain(typeof(NeedCard).GetProperties(),p=>p.Name.Contains("Address"));
    }

    [Fact]
    public async Task AttachmentRoundTripIsPrivateAndDeleteResetsReview() {
        await using var db=fixture.Open(); var user=User(); var other=User(); db.Users.AddRange(user,other);
        var profile=new TutorProfile {Id=Guid.NewGuid(),GiaSuId=user.Id,TrinhDo="Đại học",ChuyenMon="Toán",KinhNghiem="Dạy học",MoTa="Giới thiệu",KhuVuc="Thành phố",TaoLuc=DateTimeOffset.UtcNow};
        db.TutorProfiles.Add(profile); await db.SaveChangesAsync(); var environment=new TestEnvironment(); var service=new ProfileDataService(db,environment);
        try {
            var bytes="%PDF-1.4\nTest document"u8.ToArray(); using var stream=new MemoryStream(bytes);
            await service.UploadAsync(user.Id,new FormFile(stream,0,bytes.Length,"file","bang-cap.pdf"));
            var file=Assert.Single(await service.FilesAsync(user.Id));
            Assert.Empty(await service.FilesAsync(other.Id)); Assert.Null(await service.DownloadAsync(other.Id,file.Id));
            Assert.Equal(bytes,(await service.DownloadAsync(user.Id,file.Id))!.Value.Bytes);
            await service.DeleteAsync(other.Id,file.Id); Assert.Single(await service.FilesAsync(user.Id));
            await service.DeleteAsync(user.Id,file.Id); Assert.Empty(await service.FilesAsync(user.Id));
            Assert.Null(await service.DownloadAsync(user.Id,file.Id)); Assert.Equal(ProfileReviewState.Pending,profile.TrangThaiDuyet);
        } finally {if(Directory.Exists(environment.ContentRootPath)) Directory.Delete(environment.ContentRootPath,true);}
    }
    [Theory]
    [InlineData("fake.pdf","not a pdf")]
    [InlineData("script.html","%PDF-1.4")]
    [InlineData("empty.pdf","")]
    public void InvalidAttachmentsAreRejected(string name,string data) => Assert.Throws<InvalidOperationException>(()=>ProfileDataService.ValidateFile(name,System.Text.Encoding.UTF8.GetBytes(data)));

    [Fact]
    public async Task MissingAddressCannotBeSavedOrBypassedWhenCreatingNeed() {
        await using var db=fixture.Open(); var user=User(); db.Users.Add(user); await db.SaveChangesAsync();
        var data=new ProfileDataService(db,new TestEnvironment());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>data.SaveAddressAsync(user.Id,new(){City="Thành phố",DetailedAddress="  "}));
        var service=new WorkspaceService(db);
        var forged=new WorkspaceViewModel {HasRequiredAddress=true};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(user.Id,forged,new(){SubjectId=fixture.SubjectId,Grade="Lớp 9",Goal="Thử",Fee=100000}));
    }

    [Fact]
    public async Task DevelopmentBypassStillRequiresQualificationFile() {
        await using var db=fixture.Open(); var user=User(); db.Users.Add(user); await db.SaveChangesAsync();
        db.PersonalProfiles.Add(new(){TaiKhoanId=user.Id,ThanhPho="Thành phố",DiaChiChiTiet="Địa chỉ riêng"});
        db.TutorProfiles.Add(new(){Id=Guid.NewGuid(),GiaSuId=user.Id,TrinhDo="Đại học",ChuyenMon="Toán",KinhNghiem="Dạy học",MoTa="Giới thiệu",KhuVuc="Thành phố",TaoLuc=DateTimeOffset.UtcNow});
        await db.SaveChangesAsync();
        var configuration=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Workspace:BypassTutorApproval"]="true" }).Build();
        var service=new WorkspaceService(db,configuration,new TestEnvironment {EnvironmentName="Development"});
        var workspace=await service.GetWorkspaceAsync(user.Id,true,false); Assert.False(workspace.IsProfileComplete);
        workspace.HasQualificationFile=true;
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(user.Id,workspace,new(){SubjectId=fixture.SubjectId,Grade="Lớp 9",Goal="Thử",Fee=100000}));
        Assert.Contains("bằng cấp",error.Message);
    }
}
