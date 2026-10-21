using System.ComponentModel.DataAnnotations;
using KetNoiGiaSu.Models;
namespace KetNoiGiaSu.ViewModels;

public sealed class ConnectForm
{
    public Guid Target { get; set; }
    public Guid? NeedId { get; set; }
    [Required(ErrorMessage="Vui lòng nhập lời nhắn."),StringLength(2000)] public string Note { get; set; } = "";
}
public sealed class ClassProposalForm
{
    public Guid Id { get; set; }
    public string Version { get; set; } = "";
    [Required,StringLength(200)] public string Name { get; set; } = "";
    [Range(1,int.MaxValue)] public int SubjectId { get; set; }
    [Required,StringLength(2000)] public string Goal { get; set; } = "";
    [Range(1,7)] public int Sessions { get; set; } = 2;
    [Range(1,480)] public int Duration { get; set; } = 90;
    [Required,StringLength(2000)] public string Schedule { get; set; } = "";
    [Range(0,1)] public int Format { get; set; }
    [StringLength(500)] public string? Location { get; set; }
    [Range(typeof(decimal),"0","100000000")] public decimal Fee { get; set; }
    [Range(0,2)] public int FeeUnit { get; set; } = 1;
}
public sealed record ConnectionItem(ConnectionRequest Request,string OtherName,bool IsSender);
public sealed class ConnectionsViewModel
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public List<ConnectionItem> Items { get; set; } = [];
    public List<ContactItem> Contacts { get; set; } = [];
}
public sealed record ContactItem(ContactRequest Request, string OtherName, bool IsSender);
public sealed class ProposalViewModel
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public ClassProposalForm Form { get; set; } = new();
}
public sealed record ConversationItem(Guid Id,string Name,string? LastMessage,DateTimeOffset Created);
public sealed class MessagesViewModel
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public Guid? Selected { get; set; }
    public string Name { get; set; } = "";
    public List<ConversationItem> Conversations { get; set; } = [];
    public List<ChatMessage> Messages { get; set; } = [];
    public Guid Owner { get; set; }
    public int Page { get; set; } = 1;
}
public sealed class SendMessageForm
{
    public Guid ConversationId { get; set; }
    public Guid OperationId { get; set; }
    [Required,StringLength(4000)] public string Text { get; set; } = "";
}
public sealed class ClassesViewModel
{
    public WorkspaceViewModel Workspace { get; set; } = new();
    public List<ConnectedClass> Classes { get; set; } = [];
}
