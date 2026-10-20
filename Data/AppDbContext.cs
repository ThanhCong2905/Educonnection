using KetNoiGiaSu.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Data;

// Maps the existing SQL schema. Do not call EnsureCreated or Migrate on this database.
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TutorProfile> TutorProfiles => Set<TutorProfile>();
    public DbSet<TeachingSubject> TeachingSubjects => Set<TeachingSubject>();
    public DbSet<LearningNeed> Needs => Set<LearningNeed>();
    public DbSet<AvailabilitySlot> Availability => Set<AvailabilitySlot>();
    public DbSet<PersonalProfile> PersonalProfiles => Set<PersonalProfile>();
    public DbSet<ProfileAttachment> ProfileAttachments => Set<ProfileAttachment>();
    public DbSet<ConnectionRequest> ConnectionRequests => Set<ConnectionRequest>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ConnectedClass> ConnectedClasses => Set<ConnectedClass>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ConnectionRequest>(e => { e.ToTable("YeuCauKetNoiLopHoc"); e.Property(x => x.RowVersion).IsRowVersion(); e.Property(x => x.HocPhi).HasPrecision(18,0); });
        builder.Entity<Conversation>().ToTable("CuocHoiThoai");
        builder.Entity<ChatMessage>().ToTable("TinNhan");
        builder.Entity<ConnectedClass>(e => { e.ToTable("LopHoc"); e.Property(x => x.RowVersion).IsRowVersion(); e.Property(x => x.HocPhi).HasPrecision(18,0); });
        builder.Entity<PersonalProfile>(e => {
            e.ToTable("HoSoCaNhan"); e.HasKey(x => x.TaiKhoanId); e.Ignore(x => x.PublicRegion);
            e.Property(x => x.ThanhPho).HasMaxLength(100); e.Property(x => x.QuanHuyen).HasMaxLength(100);
            e.Property(x => x.DiaChiChiTiet).HasMaxLength(500);
        });
        builder.Entity<ProfileAttachment>(e => { e.ToTable("TepDinhKem"); });
        builder.Entity<Subject>().ToTable("MonHoc");
        builder.Entity<TutorProfile>(e =>
        {
            e.ToTable("HoSoGiaSu");
            e.Property(x => x.HocPhiCongKhai).HasPrecision(18, 0);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        builder.Entity<TeachingSubject>(e =>
        {
            e.ToTable("HoSoGiaSuMonDay");
            e.HasKey(x => new { x.HoSoGiaSuId, x.MonHocId });
        });
        builder.Entity<LearningNeed>(e =>
        {
            e.ToTable("NhuCauHoc");
            e.Property(x => x.HocPhiDuKien).HasPrecision(18, 0);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        builder.Entity<AvailabilitySlot>().ToTable("KhungGioRanh");
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.HoTen).HasMaxLength(150).IsRequired();
            entity.Property(x => x.AnhDaiDien).HasMaxLength(500);
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.NormalizedEmail).IsRequired();
        });
        builder.Entity<IdentityUserLogin<Guid>>().Property(x => x.LoginProvider).HasMaxLength(128);
        builder.Entity<IdentityUserLogin<Guid>>().Property(x => x.ProviderKey).HasMaxLength(128);
        builder.Entity<IdentityUserToken<Guid>>().Property(x => x.LoginProvider).HasMaxLength(128);
        builder.Entity<IdentityUserToken<Guid>>().Property(x => x.Name).HasMaxLength(128);
        builder.Entity<UserSession>(entity =>
        {
            entity.ToTable("PhienDangNhap");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LyDoThuHoi).HasMaxLength(500);
            entity.Property(x => x.RowVersion).IsRowVersion();
        });
        builder.Entity<LoginAttempt>(entity =>
        {
            entity.ToTable("TrangThaiThuDangNhap");
            entity.HasKey(x => new { x.LoaiKhoa, x.GiaTriKhoa });
            entity.Property(x => x.GiaTriKhoa).HasMaxLength(256);
            entity.Property(x => x.RowVersion).IsRowVersion();
        });
    }
}
