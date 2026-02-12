using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class CreateSupportTicketRequest
{
    [Required, MinLength(1), MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;
}
