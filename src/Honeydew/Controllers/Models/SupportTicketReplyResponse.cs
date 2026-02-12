namespace Honeydew.Controllers.Models;

public class SupportTicketReplyResponse
{
    public Guid Id { get; set; }
    public Guid SupportTicketId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsFromStaff { get; set; }
    public DateTime CreatedAt { get; set; }
}
