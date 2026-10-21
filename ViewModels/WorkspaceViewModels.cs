using System.ComponentModel.DataAnnotations;
using KetNoiGiaSu.Models;

namespace KetNoiGiaSu.ViewModels;

public sealed class WorkspaceViewModel
{
    public Guid UserId { get; set; }
    public bool IsTutor { get; set; }
    public bool IsParent { get; set; }
    public bool HasRequiredAddress { get; set; }
    public bool HasQualificationFile { get; set; }
    public bool HasTutorConditions { get; set; }
    public bool IsProfileComplete => IsTutor ? MyProfile != null && HasQualificationFile && HasTutorConditions && !string.IsNullOrWhiteSpace(MyProfile.TruongDaoTao) && !string.IsNullOrWhiteSpace(MyProfile.TenHienThi) && !string.IsNullOrWhiteSpace(MyProfile.AnhDaiDien) : HasRequiredAddress;
    public TutorProfile? MyProfile { get; set; }
    public bool ApprovalBypassed { get; set; }
    public bool CanPublish => !IsTutor || (IsProfileComplete && MyProfile?.TrangThaiDuyet == ProfileReviewState.Approved);
    public string Title { get; set; } = "";
    public List<Subject> Subjects { get; set; } = [];
    public List<NeedCard> Cards { get; set; } = [];
    public List<LearningNeed> MyNeeds { get; set; } = [];
    public SearchInput Search { get; set; } = new();
    public int Total { get; set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / 9d));
}

public sealed class NeedCard
{
    public ContactRequest? Contact { get; set; }
    public string? Avatar { get; set; }
    public bool SupportsOnline { get; set; }
    public bool SupportsInPerson { get; set; }
    public bool IsTutorApproved { get; set; }
    // Supplied only by a validated compatibility calculation, never a public rating.
    public decimal? CompatibilityPercent { get; set; }
    public NeedState State { get; set; }
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? Grade { get; set; }
    public string? Goal { get; set; }
    public string? Region { get; set; }
    public int Format { get; set; }
    public decimal? Fee { get; set; }
    public int? FeeUnit { get; set; }
    public int? WeeklySessions { get; set; }
    public bool IsTutor { get; set; }
    public string? Qualification { get; set; }
    public string? Experience { get; set; }
    public DateTimeOffset Created { get; set; }
    public List<AvailabilitySlot> Slots { get; set; } = [];
    public string Initial => string.IsNullOrEmpty(OwnerName) ? "E" : OwnerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?[..1] ?? "E";
}

public sealed class SearchInput
{
    [StringLength(200)] public string? Qualification { get; set; }
    [Range(0,6)] public int? Day { get; set; }
    public TimeOnly? From { get; set; }
    public TimeOnly? To { get; set; }
    [StringLength(150)] public string? Q { get; set; }
    public int? SubjectId { get; set; }
    [StringLength(100)] public string? Grade { get; set; }
    [StringLength(200)] public string? Region { get; set; }
    [Range(0,1)] public int? Format { get; set; }
    [Range(0,100000000)] public decimal? MaxFee { get; set; }
    [Range(0,2)] public int? FeeUnit { get; set; }
    public string Sort { get; set; } = "newest";
    [Range(1,100000)] public int Page { get; set; } = 1;
}

public sealed class NeedForm
{
    [StringLength(500)] public string? DetailedAddress { get; set; }
    public Guid? Id { get; set; }
    public string? Version { get; set; }
    [Range(1,int.MaxValue,ErrorMessage="Vui lòng chọn môn học.")] public int SubjectId { get; set; }
    [Required(ErrorMessage="Vui lòng chọn khối lớp."),StringLength(100)] public string Grade { get; set; } = "";
    [Required(ErrorMessage="Vui lòng nhập mục tiêu hoặc đối tượng nhận dạy."),StringLength(2000)] public string Goal { get; set; } = "";
    [StringLength(200)] public string? Region { get; set; }
    [Range(0,1)] public int Format { get; set; }
    [Range(1,7)] public int WeeklySessions { get; set; } = 2;
    [Required(ErrorMessage="Vui lòng nhập mức học phí."),Range(typeof(decimal),"0","100000000")] public decimal? Fee { get; set; }
    [Range(0,2)] public int FeeUnit { get; set; } = 1;
    [StringLength(150)] public string? LearnerName { get; set; }
    [Range(1,100)] public int? LearnerAge { get; set; }
    public int[] Days { get; set; } = [];
    public TimeOnly Start { get; set; } = new(18, 0);
    public TimeOnly End { get; set; } = new(20, 0);
    public bool Publish { get; set; }
}

public sealed class NeedEditorViewModel
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public NeedForm Form { get; set; } = new();
}

public sealed class ProfileDetailViewModel
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public TutorProfile Profile { get; set; } = new();
    public string Name { get; set; } = "";
    public List<string> Subjects { get; set; } = [];
    public List<NeedCard> Needs { get; set; } = [];
    public Guid? SelectedNeedId { get; set; }
    public TutorConditions? Conditions { get; set; }
    public List<TutorAvailability> Availability { get; set; } = [];
    public TutorProfileRevision? LatestRevision { get; set; }
    public ContactRequest? Contact { get; set; }
}
