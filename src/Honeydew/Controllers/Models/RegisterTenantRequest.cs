using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class RegisterTenantRequest
{
    [Required, MaxLength(256)]
    public string TenantName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string OwnerEmail { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(500)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string OwnerDisplayName { get; set; } = string.Empty;
}
