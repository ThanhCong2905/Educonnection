using System.Security.Claims;
using System.Text.Json;
using KetNoiGiaSu.ViewModels;
using Microsoft.AspNetCore.Mvc;
namespace KetNoiGiaSu.Services;

public sealed class ProfileExtrasViewComponent(ProfileDataService service, WorkspaceService workspaceService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var owner = Guid.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tutor = HttpContext.User.IsInRole("GiaSu");
        var address = await service.AddressAsync(owner);
        var form = new PersonalProfileForm { City=address?.ThanhPho ?? "", District=address?.QuanHuyen, DetailedAddress=address?.DiaChiChiTiet };
        if (ViewContext.TempData["AddressDraft"] is string draft) form=JsonSerializer.Deserialize<PersonalProfileForm>(draft) ?? form;
        return View("~/Views/Shared/_ProfileExtras.cshtml",new PersonalProfileEditor {
            Workspace=await workspaceService.GetWorkspaceAsync(owner,tutor,HttpContext.User.IsInRole("PhuHuynh")),
            Form=form, Files=tutor ? await service.FilesAsync(owner) : []
        });
    }
}
