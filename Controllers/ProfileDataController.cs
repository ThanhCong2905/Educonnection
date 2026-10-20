using System.Security.Claims;
using KetNoiGiaSu.Services;
using KetNoiGiaSu.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KetNoiGiaSu.Controllers;

[Authorize(Roles="HocSinh,PhuHuynh,GiaSu"), AutoValidateAntiforgeryToken]
[ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
public sealed class ProfileDataController(ProfileDataService service) : Controller
{
    private Guid Owner => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IActionResult Profile(string section = "personal-details") => RedirectToAction("Profile", "Workspace", null, section);
    private void Errors() => TempData["ProfileError"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Dữ liệu không hợp lệ." : x.ErrorMessage));
    [HttpGet]
    public IActionResult Address() => Profile();
    [HttpPost]
    public async Task<IActionResult> Address([Bind(Prefix="Form")] PersonalProfileForm form) {
        if (ModelState.IsValid) {
            try { await service.SaveAddressAsync(Owner, form); TempData["Success"]="Đã lưu địa chỉ mặc định. Các nhu cầu đã đăng không bị thay đổi."; return Profile(); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); }
        }
        Errors(); TempData["AddressDraft"]=System.Text.Json.JsonSerializer.Serialize(form); return Profile();
    }
    [HttpGet, Authorize(Roles="GiaSu")]
    public IActionResult Documents() => Profile("qualifications-files");

    [HttpPost, Authorize(Roles="GiaSu"), RequestSizeLimit(11000000)]
    public async Task<IActionResult> Upload(IFormFile? file) {
        if (file == null) ModelState.AddModelError("", "Vui lòng chọn tệp minh chứng.");
        if (ModelState.IsValid) {
            try { await service.UploadAsync(Owner, file!); TempData["Success"]="Đã thêm minh chứng riêng tư. Hồ sơ chuyển về chờ xét duyệt."; return Profile("qualifications-files"); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); }
        }
        Errors(); return Profile("qualifications-files");
    }
    [HttpGet, Authorize(Roles="GiaSu")]
    public async Task<IActionResult> Download(Guid id) {
        var result = await service.DownloadAsync(Owner,id); if (result == null) return NotFound();
        Response.Headers["X-Content-Type-Options"]="nosniff";
        return File(result.Value.Bytes, result.Value.Metadata.LoaiNoiDung, result.Value.Metadata.TenGoc);
    }
    [HttpPost, Authorize(Roles="GiaSu")]
    public async Task<IActionResult> Delete(Guid id) {
        await service.DeleteAsync(Owner,id); TempData["Success"]="Đã xóa minh chứng."; return Profile("qualifications-files");
    }
}
