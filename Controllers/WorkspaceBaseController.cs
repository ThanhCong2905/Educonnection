using System.Security.Claims;
using KetNoiGiaSu.Models;
using KetNoiGiaSu.Services;
using KetNoiGiaSu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Controllers;

[Authorize(Roles = "HocSinh,PhuHuynh,GiaSu")]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class WorkspaceBaseController(WorkspaceService workspaceService) : Controller
{
    protected WorkspaceService WorkspaceData => workspaceService;
    protected Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected Task<WorkspaceViewModel> Workspace() => workspaceService.GetWorkspaceAsync(UserId, User.IsInRole("GiaSu"), User.IsInRole("PhuHuynh"));
}
