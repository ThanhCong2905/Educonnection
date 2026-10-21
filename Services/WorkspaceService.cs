using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class WorkspaceService(AppDbContext db, IConfiguration? configuration = null, IWebHostEnvironment? environment = null)
{
    private bool BypassApproval => environment?.IsDevelopment() == true && configuration?.GetValue<bool>("Workspace:BypassTutorApproval") == true;
    public async Task<MyAccountViewModel> MyAccountAsync(Guid owner, WorkspaceViewModel workspace)
    {
        return await db.Users.AsNoTracking().Where(x => x.Id == owner)
            .Select(x => new MyAccountViewModel(workspace, x.HoTen, x.Email, x.PhoneNumber)).SingleAsync();
    }

    public async Task<WorkspaceViewModel> GetWorkspaceAsync(Guid userId, bool tutor, bool parent)
    {
        return new WorkspaceViewModel
        {
            UserId=userId, IsTutor = tutor, IsParent = parent, ApprovalBypassed = false,
            HasRequiredAddress = await db.PersonalProfiles.AnyAsync(x => x.TaiKhoanId == userId && x.ThanhPho.Trim() != "" && x.DiaChiChiTiet != null && x.DiaChiChiTiet.Trim() != ""),
            HasQualificationFile = tutor && await db.ProfileAttachments.AnyAsync(x => x.NguoiTaiLenId == userId && x.HoSoGiaSuId != null),
            HasTutorConditions = tutor && await (from p in db.TutorProfiles join c in db.TutorConditions on p.Id equals c.HoSoGiaSuId where p.GiaSuId==userId select c).AnyAsync(),
            MyProfile = tutor ? await db.TutorProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.GiaSuId == userId) : null,
            Subjects = await db.Subjects.AsNoTracking().Where(x => x.DangSuDung).OrderBy(x => x.TenMon).ToListAsync(),
            MyNeeds = tutor ? [] : await db.Needs.AsNoTracking().Where(x => x.ChuSoHuuId == userId&&x.LoaiNhuCau==0).OrderByDescending(x => x.TaoLuc).ToListAsync()
        };
    }

}
