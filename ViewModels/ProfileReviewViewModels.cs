using KetNoiGiaSu.Models;
namespace KetNoiGiaSu.ViewModels;
public sealed record ProfileReviewItem(TutorProfileRevision Revision, string OwnerName, List<ProfileAttachment> Evidence, List<string> Subjects);
