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
public sealed class WorkspaceController(WorkspaceService service) : Controller
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Task<WorkspaceViewModel> Workspace() => service.GetWorkspaceAsync(UserId, User.IsInRole("GiaSu"), User.IsInRole("PhuHuynh"));

    [Route("/Workspace/Index")]
    [Route("/Workspace")]
    public async Task<IActionResult> Index()
    {
        var model = await Workspace(); model.Title = "Trang chủ"; return View("~/Views/Workspace/Index.cshtml", model);
    }

    [Route("/Workspace/Status")]
    public async Task<IActionResult> Status()
    {
        var model = await Workspace(); model.Title = "Trạng thái xử lý"; return View("~/Views/Workspace/Status.cshtml", model);
    }

    [Route("/Workspace/Section")]
    public async Task<IActionResult> Section(string name)
    {
        if(name=="connections") return RedirectToAction("DanhSach", "KetNoi");
        if(name=="messages") return RedirectToAction("DanhSachHoiThoai", "TinNhan");
        if(name=="classes") return RedirectToAction("DanhSach", "LopHoc");
        var names = new Dictionary<string,string> { ["connections"]="Kết nối", ["classes"]="Lớp học", ["messages"]="Tin nhắn", ["notifications"]="Thông báo" };
        if (!names.TryGetValue(name, out var title)) return NotFound();
        var model = await Workspace(); model.Title = title; return View("~/Views/Workspace/Section.cshtml", model);
    }

}
