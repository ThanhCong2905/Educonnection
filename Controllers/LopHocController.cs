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
public sealed class LopHocController(LopHocService communication, WorkspaceService workspaceService) : WorkspaceBaseController(workspaceService)
{

    [Route("/LopHoc/DanhSach")]
    [Route("/Workspace/Classes")]
    public async Task<IActionResult> DanhSach() {
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        return View("~/Views/LopHoc/DanhSach.cshtml", new ClassesViewModel {Workspace=workspace,Classes=await communication.LayDanhSachAsync(UserId)});
    }

}
