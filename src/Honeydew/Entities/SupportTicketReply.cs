namespace Honeydew.Entities;

public class SupportTicketReply
{
    public Guid Id { get; set; }
    public Guid SupportTicketId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsFromStaff { get; set; }
    public DateTime CreatedAt { get; set; }

    public SupportTicket SupportTicket { get; set; } = null!;
}
