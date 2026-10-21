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
public sealed class HoSoGiaSuController(HoSoGiaSuService service, WorkspaceService workspaceService, DieuKienNhanLopService conditions) : WorkspaceBaseController(workspaceService)
{
    [HttpGet("/HoSoGiaSu/DieuKien"),Authorize(Roles="GiaSu")]
    public async Task<IActionResult> DieuKien()
    {
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        if(workspace.MyProfile==null)return RedirectToAction(nameof(Sua));
        return View(new TutorConditionsEditor {Workspace=workspace,Form=await conditions.LayDeSuaAsync(UserId)});
    }

    [HttpPost("/HoSoGiaSu/DieuKien"),Authorize(Roles="GiaSu")]
    public async Task<IActionResult> DieuKien([Bind(Prefix="Form")] TutorConditionsForm form)
    {
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        if(workspace.MyProfile==null)return RedirectToAction(nameof(Sua));
        if(ModelState.IsValid)try {await conditions.CapNhatAsync(UserId,form);TempData["Success"]="Đã lưu điều kiện nhận lớp. Thay đổi có hiệu lực ngay, không cần duyệt lại chuyên môn.";return RedirectToAction(nameof(ChiTiet));}
        catch(Exception ex)when(ex is InvalidOperationException or DbUpdateConcurrencyException) {ModelState.AddModelError("",ex is DbUpdateConcurrencyException?"Dữ liệu đã thay đổi. Vui lòng tải lại trang.":ex.Message);}
        return View(new TutorConditionsEditor {Workspace=workspace,Form=form});
    }

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
        return View("~/Views/HoSoGiaSu/Sua.cshtml", new ProfileEditorViewModel { Workspace = workspace, Form = form, HasPendingRevision=!string.IsNullOrEmpty(form.RevisionVersion) });
    }

    [Route("/HoSoGiaSu/Luu")]
    [Route("/HoSoGiaSu/Sua")]
    [Route("/Workspace/EditProfile/{id?}")]
    [HttpPost, Authorize(Roles="GiaSu"), RequestSizeLimit(3000000)]
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
        form.CurrentAvatar=(await service.LayDeSuaAsync(workspace)).CurrentAvatar;
        return View("~/Views/HoSoGiaSu/Sua.cshtml", new ProfileEditorViewModel { Workspace = workspace, Form = form, HasPendingRevision=!string.IsNullOrEmpty(form.RevisionVersion) });
    }

}
