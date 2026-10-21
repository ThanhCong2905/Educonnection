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
public sealed class HoSoGiaSuController(HoSoGiaSuService service, WorkspaceService workspaceService, ProfileDataService profileData) : WorkspaceBaseController(workspaceService)
{

    [Route("/HoSoGiaSu/ChiTiet")]
    [Route("/Workspace/Profile/{id?}")]
    public async Task<IActionResult> ChiTiet(Guid? id, Guid? needId)
    {
        var workspace = await Workspace();
        if (!id.HasValue && !workspace.IsTutor) { ViewData["Workspace"] = workspace; return View("MyAccount", await WorkspaceData.MyAccountAsync(UserId, workspace)); }
        if (!id.HasValue && workspace.MyProfile == null) { ViewData["Workspace"] = workspace; return View("ProfilePending", workspace); }
        var model = await service.LayHoSoAsync(id ?? UserId, workspace, needId);
        if (model == null) return NotFound();
        ViewData["Workspace"] = workspace; return View("~/Views/HoSoGiaSu/ChiTiet.cshtml", model);
    }

    [Route("/HoSoGiaSu/Sua")]
    [Route("/Workspace/EditProfile/{id?}")]
    [HttpGet, Authorize(Roles="GiaSu")]
    public async Task<IActionResult> Sua()
    {
        var workspace = await Workspace(); ViewData["Workspace"] = workspace;
        var form = await service.LayDeSuaAsync(workspace);
        var address = await profileData.AddressAsync(UserId);
        if (address != null) form.Region = address.PublicRegion;
        return View("~/Views/HoSoGiaSu/Sua.cshtml", new ProfileEditorViewModel { Workspace = workspace, Form = form });
    }

    [Route("/HoSoGiaSu/Luu")]
    [Route("/HoSoGiaSu/Sua")]
    [Route("/Workspace/EditProfile/{id?}")]
    [HttpPost, Authorize(Roles="GiaSu")]
    public async Task<IActionResult> Luu([Bind(Prefix="Form")] TutorProfileForm form)
    {
        var workspace = await Workspace(); ViewData["Workspace"] = workspace;
        if (form.Subjects.Length == 0 || form.Subjects.Any(id => !workspace.Subjects.Any(s => s.Id == id))) ModelState.AddModelError("", "Vui lòng chọn ít nhất một môn học hợp lệ.");
        if (ModelState.IsValid)
        {
            try { await service.CapNhatAsync(UserId, form); TempData["Success"] = "Đã gửi hồ sơ. Bạn có thể theo dõi kết quả tại Trạng thái xử lý."; return RedirectToAction("ChiTiet", "HoSoGiaSu"); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Hồ sơ vừa thay đổi. Vui lòng tải lại trang."); }
        }
        return View("~/Views/HoSoGiaSu/Sua.cshtml", new ProfileEditorViewModel { Workspace = workspace, Form = form });
    }

}
