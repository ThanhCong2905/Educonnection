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
public sealed class TinNhanController(TinNhanService communication, WorkspaceService workspaceService) : WorkspaceBaseController(workspaceService)
{

    [Route("/TinNhan/MoHoiThoai")]
    [Route("/Workspace/OpenChat")]
    [HttpPost]
    public async Task<IActionResult> MoHoiThoai(Guid target) {
        try {var id=await communication.MoHoiThoaiAsync(UserId,target);return RedirectToAction("DanhSachHoiThoai", "TinNhan",new{id});}
        catch(InvalidOperationException ex){TempData["Error"]=ex.Message;return RedirectToAction("DanhSachHoiThoai", "TinNhan");}
    }

    [Route("/TinNhan/DanhSachHoiThoai")]
    [Route("/Workspace/Messages/{id?}")]
    public async Task<IActionResult> DanhSachHoiThoai(Guid? id,int page=1) {
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        var model=new MessagesViewModel {Workspace=workspace,Owner=UserId,Selected=id,Page=Math.Clamp(page,1,10000),Conversations=await communication.LayHoiThoaiTheoTaiKhoanAsync(UserId)};
        if(id.HasValue) {
            if(await communication.LayHoiThoaiAsync(UserId,id.Value)==null)return NotFound();
            model.Messages=await communication.LayDanhSachAsync(UserId,id.Value,model.Page);
            model.Name=model.Conversations.FirstOrDefault(x=>x.Id==id)?.Name??"Cuộc trò chuyện";
        }
        return View("~/Views/TinNhan/DanhSachHoiThoai.cshtml", model);
    }

    [Route("/TinNhan/LayDanhSach")]
    [Route("/Workspace/MessageFeed/{id?}")]
    [HttpGet]
    public async Task<IActionResult> LayDanhSach(Guid id) {
        if(await communication.LayHoiThoaiAsync(UserId,id)==null)return NotFound();
        var messages=await communication.LayDanhSachAsync(UserId,id);
        return Json(messages.Select(x=>new{id=x.Id,text=x.NoiDung,mine=x.NguoiGuiId==UserId,time=x.GuiLuc}));
    }

    [Route("/TinNhan/Gui")]
    [Route("/Workspace/SendMessage")]
    [HttpPost]
    public async Task<IActionResult> Gui(SendMessageForm form) {
        var ajax=Request.Headers.Accept.ToString().Contains("application/json");
        if(!ModelState.IsValid) {if(ajax)return BadRequest(new{error="Tin nhắn phải có từ 1 đến 4000 ký tự."});TempData["Error"]="Tin nhắn không hợp lệ.";}
        else try {await communication.GuiAsync(UserId,form);if(ajax)return Ok(new{sent=true});}
        catch(InvalidOperationException ex){if(ajax)return BadRequest(new{error=ex.Message});TempData["Error"]=ex.Message;}
        return RedirectToAction("DanhSachHoiThoai", "TinNhan",new{id=form.ConversationId});
    }

}
