using System.Security.Claims;
using KetNoiGiaSu.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KetNoiGiaSu.Controllers;

[Authorize(Roles="Admin,QuanTriVien")]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class QuanTriController(KiemDuyetHoSoService service) : Controller
{
    private Guid Actor=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("/QuanTri/HoSo")]
    public async Task<IActionResult> HoSo()=>View("~/Views/Shared/KiemDuyetHoSo.cshtml",await service.LayDanhSachAsync(Actor));
    [HttpPost("/QuanTri/DuyetHoSo")]
    public async Task<IActionResult> DuyetHoSo(Guid id,bool approve,string version,string? reason)
    {
        try {await service.DuyetAsync(Actor,id,approve,version,reason);TempData["Success"]=approve?"Đã duyệt hồ sơ.":"Đã từ chối bản cập nhật.";}
        catch(Exception ex)when(ex is InvalidOperationException or DbUpdateConcurrencyException){TempData["Error"]=ex is DbUpdateConcurrencyException?"Bản hồ sơ đã thay đổi. Vui lòng tải lại.":ex.Message;}
        return RedirectToAction(nameof(HoSo));
    }
    [HttpGet("/QuanTri/MinhChung")]
    public async Task<IActionResult> MinhChung(Guid id,Guid fileId)
    {
        var result=await service.TaiMinhChungAsync(Actor,id,fileId);if(result==null)return NotFound();
        Response.Headers["X-Content-Type-Options"]="nosniff";
        return File(result.Value.Bytes,result.Value.Metadata.LoaiNoiDung,result.Value.Metadata.TenGoc);
    }
}
