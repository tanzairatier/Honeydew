using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class UpdateTenantRequest
{
    [Required, MinLength(1), MaxLength(256)]
    public string Name { get; set; } = string.Empty;
}
