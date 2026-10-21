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
public sealed class KetNoiController(KetNoiService communication, WorkspaceService workspaceService) : WorkspaceBaseController(workspaceService)
{
    [HttpPost("/KetNoi/ChapNhan")]
    public Task<IActionResult> ChapNhan(Guid id, string version) => PhanHoi(id, "accept", version);

    [HttpPost("/KetNoi/Rut")]
    public Task<IActionResult> Rut(Guid id, string version) => PhanHoi(id, "cancel", version);

    [HttpPost("/KetNoi/TuChoi")]
    public Task<IActionResult> TuChoi(Guid id, string version) => PhanHoi(id, "reject", version);

    [Route("/KetNoi/DanhSach")]
    [Route("/Workspace/Connections")]
    public async Task<IActionResult> DanhSach() {
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        return View("~/Views/KetNoi/DanhSach.cshtml", new ConnectionsViewModel {Workspace=workspace,Items=await communication.LayDanhSachAsync(UserId)});
    }

    [Route("/KetNoi/GuiYeuCau")]
    [Route("/Workspace/Connect")]
    [HttpPost]
    public async Task<IActionResult> GuiYeuCau(ConnectForm form) {
        if(!ModelState.IsValid) TempData["Error"]="Vui lòng nhập lời nhắn không quá 2000 ký tự.";
        else try {await communication.GuiYeuCauAsync(UserId,form);TempData["Success"]="Đã gửi lời mời kết nối. Hai bên có thể trao đổi trước khi gia sư đề xuất lớp.";}
        catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction("DanhSach", "KetNoi");
    }

    [Route("/KetNoi/GuiDeXuat")]
    [Route("/Workspace/Propose/{id?}")]
    [HttpGet,Authorize(Roles="GiaSu")]
    public async Task<IActionResult> GuiDeXuat(Guid id) {
        await communication.XuLyHetHanYeuCauAsync();var request=await communication.LayYeuCauAsync(UserId,id);
        if(request==null || request.GiaSuId!=UserId) return NotFound();
        if(request.TrangThai is not (ConnectionState.Requested or ConnectionState.Proposed)) {TempData["Error"]="Yêu cầu không còn chờ đề xuất.";return RedirectToAction("DanhSach", "KetNoi");}
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        return View("~/Views/KetNoi/DeXuatLop.cshtml", new ProposalViewModel {Workspace=workspace,Form=new(){Id=id,Version=Convert.ToBase64String(request.RowVersion),Name=request.TenLopDuKien??"",SubjectId=request.MonHocId??0,Goal=request.MucTieu??"",Sessions=request.SoBuoiTuan??2,Duration=request.ThoiLuongPhut??90,Schedule=request.LichDuKien??"",Format=request.HinhThucHoc??0,Location=request.DiaDiem,Fee=request.HocPhi??0,FeeUnit=request.DonViHocPhi??1}});
    }

    [Route("/KetNoi/GuiDeXuat")]
    [Route("/Workspace/Propose/{id?}")]
    [HttpPost,Authorize(Roles="GiaSu")]
    public async Task<IActionResult> GuiDeXuat([Bind(Prefix="Form")] ClassProposalForm form) {
        if(ModelState.IsValid)try {await communication.GuiDeXuatAsync(UserId,form);TempData["Success"]="Đã gửi đề xuất lớp. Bên học có 72 giờ để xác nhận.";return RedirectToAction("DanhSach", "KetNoi");}
        catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}
        var workspace=await Workspace();ViewData["Workspace"]=workspace;return View("~/Views/KetNoi/DeXuatLop.cshtml", new ProposalViewModel {Workspace=workspace,Form=form});
    }

    [Route("/KetNoi/PhanHoi")]
    [Route("/Workspace/Respond/{id?}")]
    [HttpPost]
    public async Task<IActionResult> PhanHoi(Guid id,string decision,string version) {
        try
        {
            switch (decision)
            {
                case "accept": await communication.ChapNhanDeXuatAsync(UserId, id, version); break;
                case "cancel": await communication.RutYeuCauAsync(UserId, id, version); break;
                case "reject": await communication.TuChoiDeXuatAsync(UserId, id, version); break;
                default: return BadRequest();
            }
            TempData["Success"]=decision=="accept"?"Đã xác nhận kết nối và tạo lớp học.":"Đã cập nhật yêu cầu kết nối.";
        }
        catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction("DanhSach", "KetNoi");
    }

}
