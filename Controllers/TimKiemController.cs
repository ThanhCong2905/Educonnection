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
public sealed class TimKiemController(TimKiemService service, WorkspaceService workspaceService) : WorkspaceBaseController(workspaceService)
{

    [Route("/TimKiem/Tim")]
    [Route("/Workspace/Search")]
    public async Task<IActionResult> Tim([FromQuery] SearchInput search)
    {
        ViewData["AllowComparison"] = true;
        if (search.MaxFee.HasValue && !search.FeeUnit.HasValue)
            ModelState.AddModelError("", "Chọn đơn vị học phí khi lọc theo mức học phí tối đa.");
        if(search.From.HasValue!=search.To.HasValue || (search.From.HasValue && search.From>=search.To))
            ModelState.AddModelError("", "Nhập đủ giờ bắt đầu và kết thúc hợp lệ để lọc lịch rảnh.");
        var model = await Workspace(); model.Title = model.IsTutor ? "Tìm nhu cầu học" : "Tìm kiếm gia sư";
        model.Search = ModelState.IsValid ? search : new SearchInput();
        await service.TimKiemAsync(model); return View("~/Views/TimKiem/Index.cshtml", model);
    }

}
