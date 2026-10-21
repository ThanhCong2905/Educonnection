using KetNoiGiaSu.Models;
namespace KetNoiGiaSu.ViewModels;

public sealed class ContactPanelViewModel
{
    public Guid Owner { get; set; }
    public Guid Target { get; set; }
    public Guid? NeedId { get; set; }
    public ContactRequest? Contact { get; set; }
    public bool CanInteract { get; set; } = true;
}
