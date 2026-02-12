using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class CreateApiClientRequest
{
    [Required, MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string ClientId { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(500)]
    public string ClientSecret { get; set; } = string.Empty;
}
