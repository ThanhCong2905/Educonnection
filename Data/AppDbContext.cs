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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
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
