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
public sealed class NhuCauController(NhuCauService service, WorkspaceService workspaceService, ProfileDataService profileData, KetNoiGiaSu.Data.AppDbContext db) : WorkspaceBaseController(workspaceService)
{
    [HttpPost("/NhuCau/TaoNhuCauHoc")]
    [Authorize(Roles = "HocSinh,PhuHuynh")]
    public Task<IActionResult> TaoNhuCauHoc([Bind(Prefix="Form")] NeedForm form)
    {
        if (form.Id.HasValue) return Task.FromResult<IActionResult>(BadRequest());
        return CapNhat(form);
    }

    [HttpPost("/NhuCau/TaoNhuCauNhanLop")]
    [Authorize(Roles = "GiaSu")]
    public Task<IActionResult> TaoNhuCauNhanLop([Bind(Prefix="Form")] NeedForm form)
    {
        return Task.FromResult<IActionResult>(StatusCode(410,"Nhu cầu dạy đã được thay bằng hồ sơ gia sư."));
    }

    [Route("/NhuCau/Index")]
    [Route("/Workspace/Needs")]
    public async Task<IActionResult> Index(string? state)
    {
        if(User.IsInRole("GiaSu"))return RedirectToAction("DieuKien","HoSoGiaSu");
        var model = await Workspace(); model.Title = model.IsTutor ? "Nhu cầu dạy" : "Nhu cầu học";
        model.Cards = await service.LayDanhSachAsync(UserId);
        foreach (var card in model.Cards) card.OwnerName = User.Identity?.Name ?? "";
        if (Enum.TryParse<NeedState>(state, true, out var filter)) model.Cards = model.Cards.Where(x => x.State == filter).ToList();
        ViewData["State"] = state; return View("~/Views/NhuCau/Index.cshtml", model);
    }

    [Route("/NhuCau/ChiTiet")]
    [Route("/Workspace/Need/{id?}")]
    public async Task<IActionResult> ChiTiet(Guid id)
    {
        var workspace = await Workspace();
        if (!workspace.IsTutor) return Forbid();
        var card = await service.LayChiTietAsync(id);
        if (card == null) return NotFound();
        card.Contact=await db.ContactRequests.AsNoTracking().SingleOrDefaultAsync(x=>x.GiaSuId==UserId&&x.BenHocId==card.OwnerId);
        ViewData["Workspace"] = workspace; return View("~/Views/NhuCau/ChiTiet.cshtml", card);
    }

    [Route("/NhuCau/Sua")]
    [Route("/Workspace/EditNeed/{id?}")]
    [HttpGet]
    public async Task<IActionResult> Sua(Guid? id)
    {
        if(User.IsInRole("GiaSu"))return RedirectToAction("DieuKien","HoSoGiaSu");
        var workspace = await Workspace(); ViewData["Workspace"] = workspace;
        if (!workspace.IsProfileComplete) return View("ProfileRequired", workspace);
        if (!workspace.CanPublish) return View("ApprovalRequired", workspace);
        var form = new NeedForm();
        var address = await profileData.AddressAsync(UserId);
        if (address != null) { form.Region = address.PublicRegion; form.DetailedAddress = address.DiaChiChiTiet; }
        if (id.HasValue)
        {
            var need = await service.LayDeSuaAsync(id.Value, UserId);
            if (need == null) return NotFound();
            var slots = await service.LayKhungGioAsync(id.Value);
            form = new NeedForm { Id = need.Id, Version = Convert.ToBase64String(need.RowVersion), SubjectId = need.MonHocId,
                Grade = need.KhoiLop ?? "", Goal = need.MucTieu ?? "", Region = need.KhuVuc, Format = need.HinhThucHoc,
                WeeklySessions = need.SoBuoiTuan ?? 2, Fee = need.HocPhiDuKien, FeeUnit = need.DonViHocPhi ?? 1,
                LearnerName = need.TenNguoiHoc, LearnerAge = need.TuoiNguoiHoc, DetailedAddress = need.DiaChiChiTiet,
                Days = slots.Select(x => x.ThuTrongTuan).ToArray(), Start = slots.FirstOrDefault()?.GioBatDau ?? new(18,0),
                End = slots.FirstOrDefault()?.GioKetThuc ?? new(20,0), Publish = need.TrangThai == NeedState.Open };
        }
        return View("~/Views/NhuCau/Sua.cshtml", new NeedEditorViewModel { Workspace = workspace, Form = form });
    }

    [Route("/NhuCau/CapNhat")]
    [Route("/NhuCau/Sua")]
    [Route("/Workspace/EditNeed/{id?}")]
    [HttpPost]
    public async Task<IActionResult> CapNhat([Bind(Prefix="Form")] NeedForm form)
    {
        if(User.IsInRole("GiaSu"))return Forbid();
        var workspace = await Workspace(); ViewData["Workspace"] = workspace;
        if (!workspace.IsProfileComplete) { Response.StatusCode = 403; return View("ProfileRequired", workspace); }
        if (!workspace.CanPublish) { Response.StatusCode = 403; return View("ApprovalRequired", workspace); }
        if (!workspace.Subjects.Any(x => x.Id == form.SubjectId)) ModelState.AddModelError("Form.SubjectId", "Môn học không khả dụng.");
        if (form.Format == 1 && string.IsNullOrWhiteSpace(form.Region)) ModelState.AddModelError("Form.Region", "Vui lòng nhập khu vực học trực tiếp.");
        if (workspace.IsParent && string.IsNullOrWhiteSpace(form.LearnerName)) ModelState.AddModelError("Form.LearnerName", "Vui lòng cho biết người được học.");
        if (form.Days.Any(x => x < 0 || x > 6) || form.Start >= form.End) ModelState.AddModelError("", "Khung giờ không hợp lệ.");
        if (form.Publish && form.Days.Length == 0) ModelState.AddModelError("", "Chọn ít nhất một ngày rảnh trước khi mở nhu cầu.");
        if (ModelState.IsValid)
        {
            try
            {
                if (form.Id.HasValue) await service.CapNhatAsync(UserId, workspace, form);
                else if (workspace.IsTutor) await service.TaoNhuCauNhanLopAsync(UserId, workspace, form);
                else await service.TaoNhuCauHocAsync(UserId, workspace, form);
                TempData["Success"] = form.Publish ? "Nhu cầu đã được mở." : "Đã lưu bản nháp.";
                return RedirectToAction("Index", "NhuCau");
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Dữ liệu vừa thay đổi. Vui lòng tải lại trang."); }
        }
        return View("~/Views/NhuCau/Sua.cshtml", new NeedEditorViewModel { Workspace = workspace, Form = form });
    }

    [Route("/NhuCau/Dong")]
    [Route("/Workspace/CloseNeed/{id?}")]
    [HttpPost]
    public async Task<IActionResult> Dong(Guid id)
    {
        if(User.IsInRole("GiaSu"))return Forbid();
        await service.DongNhuCauAsync(id, UserId); TempData["Success"] = "Đã đóng nhu cầu. Dữ liệu được giữ lại trong danh sách của bạn."; return RedirectToAction("Index", "NhuCau");
    }

}
