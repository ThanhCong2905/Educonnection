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
public sealed class WorkspaceController(WorkspaceService service, ProfileDataService profileData, CommunicationService communication) : Controller
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Task<WorkspaceViewModel> Workspace() => service.GetWorkspaceAsync(UserId, User.IsInRole("GiaSu"), User.IsInRole("PhuHuynh"));

    public async Task<IActionResult> Index()
    {
        var model = await Workspace(); model.Title = "Trang chủ"; return View(model);
    }

    public async Task<IActionResult> Search([FromQuery] SearchInput search)
    {
        ViewData["AllowComparison"] = true;
        if (search.MaxFee.HasValue && !search.FeeUnit.HasValue)
            ModelState.AddModelError("", "Chọn đơn vị học phí khi lọc theo mức học phí tối đa.");
        var model = await Workspace(); model.Title = model.IsTutor ? "Tìm nhu cầu học" : "Tìm kiếm gia sư";
        model.Search = ModelState.IsValid ? search : new SearchInput();
        await service.SearchAsync(model); return View(model);
    }

    public async Task<IActionResult> Needs(string? state)
    {
        var model = await Workspace(); model.Title = model.IsTutor ? "Nhu cầu dạy" : "Nhu cầu học";
        model.Cards = await service.MyCardsAsync(UserId);
        foreach (var card in model.Cards) card.OwnerName = User.Identity?.Name ?? "";
        if (Enum.TryParse<NeedState>(state, true, out var filter)) model.Cards = model.Cards.Where(x => x.State == filter).ToList();
        ViewData["State"] = state; return View(model);
    }

    public async Task<IActionResult> Profile(Guid? id, Guid? needId)
    {
        var workspace = await Workspace();
        if (!id.HasValue && !workspace.IsTutor) { ViewData["Workspace"] = workspace; return View("MyAccount", await service.MyAccountAsync(UserId, workspace)); }
        if (!id.HasValue && workspace.MyProfile == null) { ViewData["Workspace"] = workspace; return View("ProfilePending", workspace); }
        var model = await service.ProfileAsync(id ?? UserId, workspace, needId);
        if (model == null) return NotFound();
        ViewData["Workspace"] = workspace; return View(model);
    }

    public async Task<IActionResult> Need(Guid id)
    {
        var workspace = await Workspace();
        if (!workspace.IsTutor) return Forbid();
        var card = await service.NeedAsync(id);
        if (card == null) return NotFound();
        ViewData["Workspace"] = workspace; return View(card);
    }

    [HttpGet]
    public async Task<IActionResult> EditNeed(Guid? id)
    {
        var workspace = await Workspace(); ViewData["Workspace"] = workspace;
        if (!workspace.IsProfileComplete) return View("ProfileRequired", workspace);
        if (!workspace.CanPublish) return View("ApprovalRequired", workspace);
        var form = new NeedForm();
        var address = await profileData.AddressAsync(UserId);
        if (address != null) { form.Region = address.PublicRegion; form.DetailedAddress = address.DiaChiChiTiet; }
        if (id.HasValue)
        {
            var need = await service.OwnedNeedAsync(id.Value, UserId);
            if (need == null) return NotFound();
            var slots = await service.SlotsAsync(id.Value);
            form = new NeedForm { Id = need.Id, Version = Convert.ToBase64String(need.RowVersion), SubjectId = need.MonHocId,
                Grade = need.KhoiLop ?? "", Goal = need.MucTieu ?? "", Region = need.KhuVuc, Format = need.HinhThucHoc,
                WeeklySessions = need.SoBuoiTuan ?? 2, Fee = need.HocPhiDuKien, FeeUnit = need.DonViHocPhi ?? 1,
                LearnerName = need.TenNguoiHoc, LearnerAge = need.TuoiNguoiHoc, DetailedAddress = need.DiaChiChiTiet,
                Days = slots.Select(x => x.ThuTrongTuan).ToArray(), Start = slots.FirstOrDefault()?.GioBatDau ?? new(18,0),
                End = slots.FirstOrDefault()?.GioKetThuc ?? new(20,0), Publish = need.TrangThai == NeedState.Open };
        }
        return View(new NeedEditorViewModel { Workspace = workspace, Form = form });
    }

    [HttpPost]
    public async Task<IActionResult> EditNeed([Bind(Prefix="Form")] NeedForm form)
    {
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
            try { await service.SaveAsync(UserId, workspace, form); TempData["Success"] = form.Publish ? "Nhu cầu đã được mở." : "Đã lưu bản nháp."; return RedirectToAction(nameof(Needs)); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Dữ liệu vừa thay đổi. Vui lòng tải lại trang."); }
        }
        return View(new NeedEditorViewModel { Workspace = workspace, Form = form });
    }

    [HttpPost]
    public async Task<IActionResult> CloseNeed(Guid id)
    {
        await service.CloseAsync(id, UserId); TempData["Success"] = "Đã đóng nhu cầu. Dữ liệu được giữ lại trong danh sách của bạn."; return RedirectToAction(nameof(Needs));
    }

    public async Task<IActionResult> Status()
    {
        var model = await Workspace(); model.Title = "Trạng thái xử lý"; return View(model);
    }

    [HttpGet, Authorize(Roles="GiaSu")]
    public async Task<IActionResult> EditProfile()
    {
        var workspace = await Workspace(); ViewData["Workspace"] = workspace;
        var form = await service.ProfileFormAsync(workspace);
        var address = await profileData.AddressAsync(UserId);
        if (address != null) form.Region = address.PublicRegion;
        return View(new ProfileEditorViewModel { Workspace = workspace, Form = form });
    }

    [HttpPost, Authorize(Roles="GiaSu")]
    public async Task<IActionResult> EditProfile([Bind(Prefix="Form")] TutorProfileForm form)
    {
        var workspace = await Workspace(); ViewData["Workspace"] = workspace;
        if (form.Subjects.Length == 0 || form.Subjects.Any(id => !workspace.Subjects.Any(s => s.Id == id))) ModelState.AddModelError("", "Vui lòng chọn ít nhất một môn học hợp lệ.");
        if (ModelState.IsValid)
        {
            try { await service.SaveProfileAsync(UserId, form); TempData["Success"] = "Đã gửi hồ sơ. Bạn có thể theo dõi kết quả tại Trạng thái xử lý."; return RedirectToAction(nameof(Profile)); }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Hồ sơ vừa thay đổi. Vui lòng tải lại trang."); }
        }
        return View(new ProfileEditorViewModel { Workspace = workspace, Form = form });
    }

    public async Task<IActionResult> Section(string name)
    {
        if(name=="connections") return RedirectToAction(nameof(Connections));
        if(name=="messages") return RedirectToAction(nameof(Messages));
        if(name=="classes") return RedirectToAction(nameof(Classes));
        var names = new Dictionary<string,string> { ["connections"]="Kết nối", ["classes"]="Lớp học", ["messages"]="Tin nhắn", ["notifications"]="Thông báo" };
        if (!names.TryGetValue(name, out var title)) return NotFound();
        var model = await Workspace(); model.Title = title; return View(model);
    }

    public async Task<IActionResult> Connections() {
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        return View(new ConnectionsViewModel {Workspace=workspace,Items=await communication.RequestsAsync(UserId)});
    }
    [HttpPost]
    public async Task<IActionResult> Connect(ConnectForm form) {
        if(!ModelState.IsValid) TempData["Error"]="Vui lòng nhập lời nhắn không quá 2000 ký tự.";
        else try {await communication.ConnectAsync(UserId,form);TempData["Success"]="Đã gửi lời mời kết nối. Hai bên có thể trao đổi trước khi gia sư đề xuất lớp.";}
        catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Connections));
    }
    [HttpGet,Authorize(Roles="GiaSu")]
    public async Task<IActionResult> Propose(Guid id) {
        await communication.ExpireAsync();var request=await communication.RequestAsync(UserId,id);
        if(request==null || request.GiaSuId!=UserId) return NotFound();
        if(request.TrangThai is not (ConnectionState.Requested or ConnectionState.Proposed)) {TempData["Error"]="Yêu cầu không còn chờ đề xuất.";return RedirectToAction(nameof(Connections));}
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        return View(new ProposalViewModel {Workspace=workspace,Form=new(){Id=id,Version=Convert.ToBase64String(request.RowVersion),Name=request.TenLopDuKien??"",SubjectId=request.MonHocId??0,Goal=request.MucTieu??"",Sessions=request.SoBuoiTuan??2,Duration=request.ThoiLuongPhut??90,Schedule=request.LichDuKien??"",Format=request.HinhThucHoc??0,Location=request.DiaDiem,Fee=request.HocPhi??0,FeeUnit=request.DonViHocPhi??1}});
    }
    [HttpPost,Authorize(Roles="GiaSu")]
    public async Task<IActionResult> Propose([Bind(Prefix="Form")] ClassProposalForm form) {
        if(ModelState.IsValid)try {await communication.ProposeAsync(UserId,form);TempData["Success"]="Đã gửi đề xuất lớp. Bên học có 72 giờ để xác nhận.";return RedirectToAction(nameof(Connections));}
        catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}
        var workspace=await Workspace();ViewData["Workspace"]=workspace;return View(new ProposalViewModel {Workspace=workspace,Form=form});
    }
    [HttpPost]
    public async Task<IActionResult> Respond(Guid id,string decision,string version) {
        try {await communication.RespondAsync(UserId,id,decision,version);TempData["Success"]=decision=="accept"?"Đã xác nhận kết nối và tạo lớp học.":"Đã cập nhật yêu cầu kết nối.";}
        catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Connections));
    }
    [HttpPost]
    public async Task<IActionResult> OpenChat(Guid target) {
        try {var id=await communication.OpenChatAsync(UserId,target);return RedirectToAction(nameof(Messages),new{id});}
        catch(InvalidOperationException ex){TempData["Error"]=ex.Message;return RedirectToAction(nameof(Messages));}
    }
    public async Task<IActionResult> Messages(Guid? id,int page=1) {
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        var model=new MessagesViewModel {Workspace=workspace,Owner=UserId,Selected=id,Page=Math.Clamp(page,1,10000),Conversations=await communication.ChatsAsync(UserId)};
        if(id.HasValue) {
            if(await communication.ConversationAsync(UserId,id.Value)==null)return NotFound();
            model.Messages=await communication.MessagesAsync(UserId,id.Value,model.Page);
            model.Name=model.Conversations.FirstOrDefault(x=>x.Id==id)?.Name??"Cuộc trò chuyện";
        }
        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> MessageFeed(Guid id) {
        if(await communication.ConversationAsync(UserId,id)==null)return NotFound();
        var messages=await communication.MessagesAsync(UserId,id);
        return Json(messages.Select(x=>new{id=x.Id,text=x.NoiDung,mine=x.NguoiGuiId==UserId,time=x.GuiLuc}));
    }
    [HttpPost]
    public async Task<IActionResult> SendMessage(SendMessageForm form) {
        var ajax=Request.Headers.Accept.ToString().Contains("application/json");
        if(!ModelState.IsValid) {if(ajax)return BadRequest(new{error="Tin nhắn phải có từ 1 đến 4000 ký tự."});TempData["Error"]="Tin nhắn không hợp lệ.";}
        else try {await communication.SendAsync(UserId,form);if(ajax)return Ok(new{sent=true});}
        catch(InvalidOperationException ex){if(ajax)return BadRequest(new{error=ex.Message});TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Messages),new{id=form.ConversationId});
    }
    public async Task<IActionResult> Classes() {
        var workspace=await Workspace();ViewData["Workspace"]=workspace;
        return View(new ClassesViewModel {Workspace=workspace,Classes=await communication.ClassesAsync(UserId)});
    }
}
