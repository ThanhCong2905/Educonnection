using System.ComponentModel.DataAnnotations;

namespace KetNoiGiaSu.ViewModels;

public sealed class TutorProfileForm
{
    public string? Version { get; set; }
    [Required(ErrorMessage="Vui lòng nhập trình độ."),StringLength(200)] public string Qualification { get; set; } = "";
    [Required(ErrorMessage="Vui lòng nhập chuyên môn."),StringLength(500)] public string Expertise { get; set; } = "";
    [Required(ErrorMessage="Vui lòng mô tả kinh nghiệm."),StringLength(2000)] public string Experience { get; set; } = "";
    [Required(ErrorMessage="Vui lòng giới thiệu bản thân."),StringLength(4000)] public string Introduction { get; set; } = "";
    [Required(ErrorMessage="Vui lòng nhập khu vực nhận dạy."),StringLength(200)] public string Region { get; set; } = "";
    [Range(typeof(decimal),"0","100000000")] public decimal Fee { get; set; }
    [Range(0,2)] public int FeeUnit { get; set; } = 1;
    public int[] Subjects { get; set; } = [];
    [Required,StringLength(500)] public string TeachingScope { get; set; } = "";
}

public sealed class ProfileEditorViewModel
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public TutorProfileForm Form { get; set; } = new();
}

public sealed record MyAccountViewModel(WorkspaceViewModel Workspace, string Name, string? Email, string? Phone);
