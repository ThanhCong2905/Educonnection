using System.Text.RegularExpressions;
using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.Services;
using KetNoiGiaSu.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KetNoiGiaSu.WorkspaceTests;

// Each fixture owns a new database; production/development data is never modified.
public sealed class WorkspaceDatabase : IAsyncLifetime
{
    private readonly string database = "KetNoiGiaSu_WorkspaceTests_" + Guid.NewGuid().ToString("N");
    private string Master => Environment.GetEnvironmentVariable("EDU_TEST_SQL_MASTER")
        ?? "Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True";
    public int SubjectId { get; private set; }
    public AppDbContext Open()
    {
        var connection = new SqlConnectionStringBuilder(Master) { InitialCatalog = database };
        return new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
    }
    public async Task InitializeAsync()
    {
        var sql = (await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"schema.sql"))).Replace("KetNoiGiaSu",database);
        await using var master = new SqlConnection(Master); await master.OpenAsync();
        foreach (var batch in Regex.Split(sql,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var command = new SqlCommand(batch,master); await command.ExecuteNonQueryAsync();
        }
        await using var db = Open(); var subject = new Subject { TenMon = "Toán kiểm thử", DangSuDung = true };
        var profileSql = (await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"profile-schema.sql"))).Replace("KetNoiGiaSu",database);
        foreach (var batch in Regex.Split(profileSql,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase)) {
            if (!string.IsNullOrWhiteSpace(batch)) { await using var command = new SqlCommand(batch,master); await command.ExecuteNonQueryAsync(); }
        }
        db.Subjects.Add(subject); await db.SaveChangesAsync(); SubjectId = subject.Id;
    }
    public async Task DisposeAsync()
    {
        if (!Regex.IsMatch(database,@"^KetNoiGiaSu_WorkspaceTests_[0-9a-f]{32}$")) throw new InvalidOperationException("Unsafe test database name.");
        SqlConnection.ClearAllPools();
        await using var master = new SqlConnection(Master); await master.OpenAsync();
        await using var command = new SqlCommand($"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END",master);
        await command.ExecuteNonQueryAsync();
    }
}

public sealed class WorkspaceServiceTests(WorkspaceDatabase fixture) : IClassFixture<WorkspaceDatabase>
{
    private static async Task<Guid> User(AppDbContext db)
    {
        var id=Guid.NewGuid();var email=$"{id:N}@example.invalid";
        db.Users.Add(new ApplicationUser { Id=id,HoTen="Người kiểm thử",Email=email,NormalizedEmail=email.ToUpperInvariant(),UserName=email,NormalizedUserName=email.ToUpperInvariant(),SecurityStamp=Guid.NewGuid().ToString() });
        await db.SaveChangesAsync();
        db.PersonalProfiles.Add(new PersonalProfile { TaiKhoanId=id,ThanhPho="Thành phố thử",QuanHuyen="Khu vực thử",DiaChiChiTiet="Địa chỉ riêng tư" });
        await db.SaveChangesAsync();return id;
    }
    private NeedForm Form() => new() { SubjectId=fixture.SubjectId,Grade="Lớp 9",Goal="Củng cố Toán lớp 9",Fee=200000,FeeUnit=1,WeeklySessions=2,Days=[1,3],Start=new(18,0),End=new(20,0),Publish=true };

    [Fact]
    public async Task PublicTutorListingsRequireApprovalAndActiveAccount()
    {
        await using var db=fixture.Open();var tutor=await User(db);var reviewer=await User(db);
        var marker=Guid.NewGuid().ToString("N");
        var profile=new TutorProfile {Id=Guid.NewGuid(),GiaSuId=tutor,TrinhDo="Cử nhân",ChuyenMon="Toán",KinhNghiem="Giảng dạy",MoTa="Giới thiệu",KhuVuc="Hà Nội",HocPhiCongKhai=200000,DonViHocPhi=1,TaoLuc=DateTimeOffset.UtcNow};
        db.TutorProfiles.Add(profile);
        db.Needs.Add(new LearningNeed {Id=Guid.NewGuid(),ChuSoHuuId=tutor,LoaiNhuCau=1,MonHocId=fixture.SubjectId,KhoiLop="Lớp 9",MucTieu=marker,HocPhiDuKien=200000,DonViHocPhi=1,TrangThai=NeedState.Open,TaoLuc=DateTimeOffset.UtcNow});
        await db.SaveChangesAsync();var service=new WorkspaceService(db);var workspace=new WorkspaceViewModel {Search=new(){Q=marker}};
        await service.SearchAsync(workspace);Assert.Empty(workspace.Cards);
        profile.TrangThaiDuyet=ProfileReviewState.Approved;profile.NguoiDuyetId=reviewer;profile.DuyetLuc=DateTimeOffset.UtcNow;await db.SaveChangesAsync();
        await service.SearchAsync(workspace);Assert.Single(workspace.Cards);
        var user=await db.Users.SingleAsync(x=>x.Id==tutor);user.TrangThai=1;await db.SaveChangesAsync();
        await service.SearchAsync(workspace);Assert.Empty(workspace.Cards);
    }

    [Fact]
    public async Task EditingApprovedProfileRequiresNewReview()
    {
        await using var db=fixture.Open();var tutor=await User(db);var reviewer=await User(db);
        var profile=new TutorProfile {Id=Guid.NewGuid(),GiaSuId=tutor,TrinhDo="Cử nhân",ChuyenMon="Toán",KinhNghiem="Giảng dạy",MoTa="Giới thiệu",KhuVuc="Hà Nội",HocPhiCongKhai=200000,DonViHocPhi=1,TaoLuc=DateTimeOffset.UtcNow,TrangThaiDuyet=ProfileReviewState.Approved,NguoiDuyetId=reviewer,DuyetLuc=DateTimeOffset.UtcNow};
        db.TutorProfiles.Add(profile);await db.SaveChangesAsync();var service=new WorkspaceService(db);
        await service.SaveProfileAsync(tutor,new(){Version=Convert.ToBase64String(profile.RowVersion),Qualification="Cử nhân",Expertise="Toán THCS",Experience="Kinh nghiệm mới",Introduction="Giới thiệu mới",Region="Hà Nội",Subjects=[fixture.SubjectId],TeachingScope="Lớp 9"});
        db.ChangeTracker.Clear();var updated=await db.TutorProfiles.SingleAsync(x=>x.Id==profile.Id);
        Assert.Equal(ProfileReviewState.Pending,updated.TrangThaiDuyet);Assert.Null(updated.NguoiDuyetId);Assert.Null(updated.DuyetLuc);
    }

    [Theory]
    [InlineData(null)] [InlineData(ProfileReviewState.Pending)] [InlineData(ProfileReviewState.Rejected)]
    public async Task UnapprovedTutorCannotCreateNeed(ProfileReviewState? state)
    {
        await using var db=fixture.Open();var owner=await User(db);var service=new WorkspaceService(db);
        var workspace=new WorkspaceViewModel {IsTutor=true,MyProfile=state.HasValue?new TutorProfile { TrangThaiDuyet=state.Value }:null};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(owner,workspace,Form()));
        Assert.False(await db.Needs.AnyAsync(x=>x.ChuSoHuuId==owner));
    }

    [Fact]
    public async Task LockedAccountCannotSaveEvenWithPreviouslyLoadedWorkspace()
    {
        await using var db=fixture.Open();var owner=await User(db);var service=new WorkspaceService(db);
        var workspace=await service.GetWorkspaceAsync(owner,false,false);
        var user=await db.Users.SingleAsync(x=>x.Id==owner);user.TrangThai=1;await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(owner,workspace,Form()));
        Assert.False(await db.Needs.AnyAsync(x=>x.ChuSoHuuId==owner));
    }

    [Fact]
    public async Task LearningNeedAndAvailabilityCommitTogether()
    {
        await using var db=fixture.Open();var owner=await User(db);var service=new WorkspaceService(db);
        var workspace=await service.GetWorkspaceAsync(owner,false,true);var form=Form();form.LearnerName="Tên riêng tư";
        await service.SaveAsync(owner,workspace,form);
        var need=await db.Needs.SingleAsync(x=>x.ChuSoHuuId==owner);
        Assert.Equal(NeedState.Open,need.TrangThai);Assert.Equal("Tên riêng tư",need.TenNguoiHoc);
        Assert.Equal(2,await db.Availability.CountAsync(x=>x.NhuCauId==need.Id));
        var publicDetail=await service.NeedAsync(need.Id);Assert.NotNull(publicDetail);Assert.Equal("Người kiểm thử",publicDetail.OwnerName);
        Assert.DoesNotContain("Tên riêng tư",publicDetail.OwnerName);
    }

    [Fact]
    public async Task FailedAvailabilityInsertRollsBackNeed()
    {
        await using var db=fixture.Open();var owner=await User(db);var service=new WorkspaceService(db);
        var form=Form();form.Days=[99];
        await Assert.ThrowsAsync<DbUpdateException>(()=>service.SaveAsync(owner,new(),form));
        db.ChangeTracker.Clear();Assert.False(await db.Needs.AnyAsync(x=>x.ChuSoHuuId==owner));
    }

    [Fact]
    public async Task AnotherAccountCannotEditOrCloseNeed()
    {
        await using var db=fixture.Open();var owner=await User(db);var other=await User(db);var service=new WorkspaceService(db);
        await service.SaveAsync(owner,new(),Form());var need=await db.Needs.AsNoTracking().SingleAsync(x=>x.ChuSoHuuId==owner);
        var edit=Form();edit.Id=need.Id;edit.Version=Convert.ToBase64String(need.RowVersion);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(other,new(),edit));
        await service.CloseAsync(need.Id,other);db.ChangeTracker.Clear();
        Assert.Equal(NeedState.Open,(await db.Needs.SingleAsync(x=>x.Id==need.Id)).TrangThai);
    }

    [Fact]
    public async Task StaleVersionCannotOverwriteNewerEdit()
    {
        await using var db=fixture.Open();var owner=await User(db);var service=new WorkspaceService(db);
        await service.SaveAsync(owner,new(),Form());var need=await db.Needs.AsNoTracking().SingleAsync(x=>x.ChuSoHuuId==owner);
        var edit=Form();edit.Id=need.Id;edit.Version=Convert.ToBase64String(need.RowVersion);edit.Goal="Bản cập nhật mới";
        await service.SaveAsync(owner,new(),edit);db.ChangeTracker.Clear();edit.Goal="Bản cũ không được ghi";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(owner,new(),edit));
        Assert.Equal("Bản cập nhật mới",(await db.Needs.AsNoTracking().SingleAsync(x=>x.Id==need.Id)).MucTieu);
    }

    [Fact]
    public async Task DraftIsPrivateAndCloseRemovesPublicListing()
    {
        await using var db=fixture.Open();var owner=await User(db);var service=new WorkspaceService(db);var form=Form();form.Publish=false;
        await service.SaveAsync(owner,new(),form);var need=await db.Needs.AsNoTracking().SingleAsync(x=>x.ChuSoHuuId==owner);
        Assert.Null(await service.NeedAsync(need.Id));
        form.Id=need.Id;form.Version=Convert.ToBase64String(need.RowVersion);form.Publish=true;await service.SaveAsync(owner,new(),form);
        Assert.NotNull(await service.NeedAsync(need.Id));await service.CloseAsync(need.Id,owner);Assert.Null(await service.NeedAsync(need.Id));
    }

    [Fact]
    public async Task FakeApprovedViewModelCannotBypassDatabaseApproval()
    {
        await using var db=fixture.Open();var owner=await User(db);var service=new WorkspaceService(db);
        var forged=new WorkspaceViewModel {IsTutor=true,MyProfile=new TutorProfile {TrangThaiDuyet=ProfileReviewState.Approved}};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(owner,forged,Form()));
        Assert.False(await db.Needs.AnyAsync(x=>x.ChuSoHuuId==owner));
    }
}
