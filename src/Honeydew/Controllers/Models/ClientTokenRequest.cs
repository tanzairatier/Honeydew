using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class ClientTokenRequest
{
    [Required, MaxLength(128)]
    public string ClientId { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ClientSecret { get; set; } = string.Empty;
}
