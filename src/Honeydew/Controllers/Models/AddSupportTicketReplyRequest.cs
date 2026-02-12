using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class AddSupportTicketReplyRequest
{
    [Required, MinLength(1), MaxLength(4000)]
    public string Body { get; set; } = string.Empty;
}
