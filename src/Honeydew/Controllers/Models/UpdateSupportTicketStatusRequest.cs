using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class UpdateSupportTicketStatusRequest
{
    [Required, MaxLength(32)]
    public string Status { get; set; } = "Open";
}
