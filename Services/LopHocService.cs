using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class LopHocService(AppDbContext db)
{
    public Task<List<ConnectedClass>> LayDanhSachAsync(Guid actor) => db.ConnectedClasses.AsNoTracking().Where(x=>x.GiaSuId==actor || x.BenHocId==actor).OrderByDescending(x=>x.TaoLuc).Take(100).ToListAsync();

}
