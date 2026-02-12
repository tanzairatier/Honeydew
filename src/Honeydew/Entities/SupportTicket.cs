namespace Honeydew.Entities;

public class SupportTicket
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    /// <summary>Open or Closed.</summary>
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<SupportTicketReply> Replies { get; set; } = new List<SupportTicketReply>();
}
