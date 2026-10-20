using System.ComponentModel.DataAnnotations;
using KetNoiGiaSu.Models;
namespace KetNoiGiaSu.ViewModels;

public sealed class PersonalProfileForm
{
    [Required, StringLength(100)] public string City { get; set; } = "";
    [StringLength(100)] public string? District { get; set; }
    [Required(ErrorMessage="Vui lòng nhập địa chỉ chi tiết."), StringLength(500)] public string? DetailedAddress { get; set; }
}
public sealed class PersonalProfileEditor
{
    public List<ProfileAttachment> Files { get; set; } = [];
    public WorkspaceViewModel Workspace { get; set; } = new();
    public PersonalProfileForm Form { get; set; } = new();
}
public sealed class DocumentsViewModel
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public List<ProfileAttachment> Files { get; set; } = [];
}
