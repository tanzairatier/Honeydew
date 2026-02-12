using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class CreateUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(500)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Role { get; set; } = "Member";

    public bool CanViewAllTodos { get; set; }
    public bool CanEditAllTodos { get; set; }
    public bool CanCreateUser { get; set; }
}
