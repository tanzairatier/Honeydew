using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class AuthRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Required when the user belongs to multiple tenants.</summary>
    public Guid? TenantId { get; set; }
}
