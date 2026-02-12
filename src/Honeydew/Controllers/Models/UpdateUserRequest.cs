namespace Honeydew.Controllers.Models;

public class UpdateUserRequest
{
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
    public bool? CanViewAllTodos { get; set; }
    public bool? CanEditAllTodos { get; set; }
    public bool? CanCreateUser { get; set; }
    public bool? IsActive { get; set; }
}
