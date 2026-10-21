using System.Security.Cryptography;
using System.Text.Json;
using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class ProfileDataService(AppDbContext db, IWebHostEnvironment environment)
{
    private string Storage => Path.Combine(environment.ContentRootPath, ".private-uploads");
    public Task<PersonalProfile?> AddressAsync(Guid owner) => db.PersonalProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.TaiKhoanId == owner);

    private async Task LockActiveAsync(Guid owner)
    {
        var users = await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.AspNetUsers WITH (UPDLOCK,HOLDLOCK) WHERE Id={owner}").ToListAsync();
        if (users.SingleOrDefault()?.TrangThai != 0) throw new InvalidOperationException("Tài khoản không còn hoạt động.");
    }

    public async Task SaveAddressAsync(Guid owner, PersonalProfileForm form)
    {
        if (string.IsNullOrWhiteSpace(form.City) || string.IsNullOrWhiteSpace(form.DetailedAddress) || form.City.Length > 100 || form.District?.Length > 100 || form.DetailedAddress?.Length > 500)
            throw new InvalidOperationException("Vui lòng kiểm tra lại địa chỉ.");
        if (string.Join(", ", new[] { form.District?.Trim(), form.City.Trim() }.Where(x => !string.IsNullOrWhiteSpace(x))).Length > 200)
            throw new InvalidOperationException("Tổng độ dài khu vực và tỉnh/thành phố không được vượt quá 200 ký tự.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        await LockActiveAsync(owner);
        var profile = await db.PersonalProfiles.SingleOrDefaultAsync(x => x.TaiKhoanId == owner);
        if (profile == null) { profile = new() { TaiKhoanId = owner }; db.PersonalProfiles.Add(profile); }
        profile.ThanhPho = form.City.Trim(); profile.QuanHuyen = form.District?.Trim() ?? "";
        profile.DiaChiChiTiet = form.DetailedAddress?.Trim();
        // Only the general area is copied to the public tutor profile.
        var tutor = await db.TutorProfiles.SingleOrDefaultAsync(x => x.GiaSuId == owner);
        if (tutor != null) tutor.KhuVuc = profile.PublicRegion;
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }

    public Task<List<ProfileAttachment>> FilesAsync(Guid owner) => db.ProfileAttachments.AsNoTracking()
        .Where(x => x.NguoiTaiLenId == owner && x.HoSoGiaSuId != null).OrderByDescending(x => x.TaoLuc).ToListAsync();

    public static string ValidateFile(string name, byte[] bytes)
    {
        if (bytes.Length is < 1 or > 10000000) throw new InvalidOperationException("Mỗi tệp phải từ 1 byte đến 10 MB.");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (extension == ".pdf" && bytes.AsSpan().StartsWith("%PDF-"u8)) return "application/pdf";
        if (extension is ".jpg" or ".jpeg" && bytes.AsSpan().StartsWith(new byte[] {255,216,255})) return "image/jpeg";
        if (extension == ".png" && bytes.AsSpan().StartsWith(new byte[] {137,80,78,71,13,10,26,10})) return "image/png";
        throw new InvalidOperationException("Chỉ nhận PDF, JPG, PNG có nội dung phù hợp với đuôi tệp.");
    }

    public async Task UploadAsync(Guid owner, IFormFile file)
    {
        if (file.Length is < 1 or > 10000000) throw new InvalidOperationException("Mỗi tệp phải từ 1 byte đến 10 MB.");
        // Bound reads even if a custom IFormFile supplies an incorrect Length.
        await using var source = file.OpenReadStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920]; int read;
        while ((read = await source.ReadAsync(chunk)) != 0) {
            if (buffer.Length + read > 10000000) throw new InvalidOperationException("Tệp vượt quá 10 MB.");
            buffer.Write(chunk, 0, read);
        }
        var bytes = buffer.ToArray();
        var name = Path.GetFileName(file.FileName.Replace('\\','/'));
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || name.Any(char.IsControl)) throw new InvalidOperationException("Tên tệp không hợp lệ.");
        var contentType = ValidateFile(name, bytes);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await LockActiveAsync(owner);
        var profile = await db.TutorProfiles.SingleOrDefaultAsync(x => x.GiaSuId == owner);
        if (profile == null) throw new InvalidOperationException("Hãy lưu hồ sơ chuyên môn trước khi thêm minh chứng.");
        if (await db.ProfileAttachments.CountAsync(x => x.HoSoGiaSuId == profile.Id) >= 10) throw new InvalidOperationException("Mỗi hồ sơ tối đa 10 tệp minh chứng.");
        var stored = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Storage); var path = Path.Combine(Storage, stored);
        try {
            await File.WriteAllBytesAsync(path, bytes);
            db.ProfileAttachments.Add(new() { Id = Guid.NewGuid(), NguoiTaiLenId = owner, HoSoGiaSuId = profile.Id,
                TenGoc = name, TenLuuTru = stored, LoaiNoiDung = contentType, KichThuoc = bytes.Length,
                Checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), TrangThaiQuet = 0, TaoLuc = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            await RefreshPendingEvidenceAsync(profile.Id);
            await transaction.CommitAsync();
        } catch { if (File.Exists(path)) File.Delete(path); throw; }
    }

    public async Task<(ProfileAttachment Metadata, byte[] Bytes)?> DownloadAsync(Guid owner, Guid id)
    {
        var file = await db.ProfileAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.NguoiTaiLenId == owner && x.HoSoGiaSuId != null);
        if (file == null || !Guid.TryParseExact(file.TenLuuTru, "N", out _)) return null;
        var path = Path.Combine(Storage, file.TenLuuTru);
        if (!File.Exists(path)) return null;
        return (file, await File.ReadAllBytesAsync(path));
    }

    public async Task DeleteAsync(Guid owner, Guid id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(); await LockActiveAsync(owner);
        var file = await db.ProfileAttachments.SingleOrDefaultAsync(x => x.Id == id && x.NguoiTaiLenId == owner && x.HoSoGiaSuId != null);
        if (file == null) return;
        var profile = await db.TutorProfiles.SingleAsync(x => x.Id == file.HoSoGiaSuId);
        var approvedEvidence=await db.TutorProfileRevisions.AsNoTracking().Where(x=>x.HoSoGiaSuId==profile.Id&&x.TrangThai==ProfileReviewState.Approved).Select(x=>x.MinhChungJson).ToListAsync();
        if(approvedEvidence.Any(json=>(JsonSerializer.Deserialize<List<Guid>>(json)??[]).Contains(id)))
            throw new InvalidOperationException("Minh chứng đã dùng để duyệt hồ sơ được giữ lại. Bạn có thể bổ sung minh chứng mới.");
        if(approvedEvidence.Count==0) {profile.TrangThaiDuyet = ProfileReviewState.Pending; profile.NguoiDuyetId = null; profile.DuyetLuc = null; profile.LyDoTuChoi = null;}
        db.ProfileAttachments.Remove(file); await db.SaveChangesAsync(); await RefreshPendingEvidenceAsync(profile.Id); await transaction.CommitAsync();
        if (Guid.TryParseExact(file.TenLuuTru, "N", out _)) File.Delete(Path.Combine(Storage, file.TenLuuTru));
    }

    private async Task RefreshPendingEvidenceAsync(Guid profileId)
    {
        var revision=await db.TutorProfileRevisions.SingleOrDefaultAsync(x=>x.HoSoGiaSuId==profileId&&x.TrangThai==ProfileReviewState.Pending);
        if(revision==null)return;
        revision.MinhChungJson=JsonSerializer.Serialize(await db.ProfileAttachments.Where(x=>x.HoSoGiaSuId==profileId).Select(x=>x.Id).ToListAsync());
        revision.TaoLuc=DateTimeOffset.UtcNow;await db.SaveChangesAsync();
    }
}
