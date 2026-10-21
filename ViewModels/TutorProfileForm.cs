using System.ComponentModel.DataAnnotations;

namespace KetNoiGiaSu.ViewModels;

public sealed class TutorProfileForm
{
    public string? Version { get; set; }
    public string? RevisionVersion { get; set; }
    [Required, StringLength(150)] public string DisplayName { get; set; } = "";
    [Required, StringLength(200)] public string School { get; set; } = "";
    [Required, StringLength(200)] public string Major { get; set; } = "";
    [Required, RegularExpression("^(DangHoc|DaTotNghiep)$")] public string StudyStatus { get; set; } = "DangHoc";
    public IFormFile? Avatar { get; set; }
    public string? CurrentAvatar { get; set; }
    [Required(ErrorMessage="Vui lòng nhập trình độ."),StringLength(200)] public string Qualification { get; set; } = "";
    [Required(ErrorMessage="Vui lòng nhập chuyên môn."),StringLength(500)] public string Expertise { get; set; } = "";
    [Required(ErrorMessage="Vui lòng mô tả kinh nghiệm."),StringLength(2000)] public string Experience { get; set; } = "";
    [Required(ErrorMessage="Vui lòng giới thiệu bản thân."),StringLength(4000)] public string Introduction { get; set; } = "";
    [StringLength(200)] public string Region { get; set; } = "";
    [Range(typeof(decimal),"0","100000000")] public decimal Fee { get; set; }
    [Range(0,2)] public int FeeUnit { get; set; } = 1;
    public int[] Subjects { get; set; } = [];
    [Required,StringLength(500)] public string TeachingScope { get; set; } = "";
}

public sealed class ProfileEditorViewModel
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public TutorProfileForm Form { get; set; } = new();
    public bool HasPendingRevision { get; set; }
}

public sealed class TutorSlotForm
{
    public bool Enabled { get; set; }
    [Range(0,6)] public int Day { get; set; }
    public TimeOnly Start { get; set; } = new(18,0);
    public TimeOnly End { get; set; } = new(20,0);
}
public sealed class TutorConditionsForm
{
    public string? Version { get; set; }
    public bool Online { get; set; } = true;
    public bool InPerson { get; set; }
    [StringLength(200)] public string? Region { get; set; }
    [Range(typeof(decimal),"0","100000000")] public decimal? Fee { get; set; }
    [Range(0,2)] public int FeeUnit { get; set; } = 1;
    public bool AcceptingStudents { get; set; }
    public List<TutorSlotForm> Slots { get; set; } = Enumerable.Range(0,7).Select(d=>new TutorSlotForm {Day=d}).ToList();
}
public sealed class TutorConditionsEditor
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public TutorConditionsForm Form { get; set; } = new();
}

public sealed record MyAccountViewModel(WorkspaceViewModel Workspace, string Name, string? Email, string? Phone);
