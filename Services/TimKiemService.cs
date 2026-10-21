using KetNoiGiaSu.Data;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Services;

public sealed class TimKiemService(NhuCauService nhuCau)
{
    public async Task TimKiemAsync(WorkspaceViewModel workspace)
    {
        var f = workspace.Search;
        var q = nhuCau.Catalog(!workspace.IsTutor).Where(x => x.State == NeedState.Open);
        if (!string.IsNullOrWhiteSpace(f.Q)) { var word = f.Q.Trim(); q = q.Where(x => x.OwnerName.Contains(word) || x.Subject.Contains(word) || (x.Goal != null && x.Goal.Contains(word))); }
        if (f.SubjectId.HasValue)
        {
            var subject = workspace.Subjects.FirstOrDefault(x => x.Id == f.SubjectId)?.TenMon;
            q = q.Where(x => x.Subject == subject);
        }
        if (!string.IsNullOrWhiteSpace(f.Grade)) q = q.Where(x => x.Grade == f.Grade);
        if (!string.IsNullOrWhiteSpace(f.Region)) q = q.Where(x => x.Region != null && x.Region.Contains(f.Region));
        if (f.Format.HasValue) q = q.Where(x => x.Format == f.Format);
        if (f.FeeUnit.HasValue) q = q.Where(x => x.FeeUnit == f.FeeUnit);
        if (f.MaxFee.HasValue) { var unit = f.FeeUnit ?? 1; q = q.Where(x => x.FeeUnit == unit && x.Fee <= f.MaxFee); }
        workspace.Total = await q.CountAsync();
        f.Page = Math.Clamp(f.Page, 1, workspace.PageCount);
        workspace.Cards = await q.OrderByDescending(x => x.Created).ThenBy(x => x.Id).Skip((f.Page - 1) * 9).Take(9).ToListAsync();
    }

}
