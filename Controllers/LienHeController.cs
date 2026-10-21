using KetNoiGiaSu.Services;
using KetNoiGiaSu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KetNoiGiaSu.Controllers;

[Authorize(Roles="HocSinh,PhuHuynh,GiaSu")]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class LienHeController(LienHeService service,WorkspaceService workspaceService) : WorkspaceBaseController(workspaceService)
{
    [HttpPost("/LienHe/Gui")]
    public async Task<IActionResult> Gui(ConnectForm form)
    {
        if(!ModelState.IsValid)TempData["Error"]="Vui lòng nhập lời nhắn liên hệ tối đa 2000 ký tự.";
        else try {await service.GuiAsync(UserId,form);TempData["Success"]="Đã gửi yêu cầu liên hệ. Chờ người nhận chấp nhận để nhắn tin và kết nối.";}
        catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction("DanhSach","KetNoi");
    }
    [HttpPost("/LienHe/PhanHoi")]
    public async Task<IActionResult> PhanHoi(Guid id,string decision,string version)
    {
        try {await service.PhanHoiAsync(UserId,id,decision,version);TempData["Success"]=decision=="accept"?"Đã chấp nhận liên hệ. Hai bên có thể nhắn tin và gửi yêu cầu kết nối.":"Đã cập nhật yêu cầu liên hệ.";}
        catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction("DanhSach","KetNoi");
    }
}
