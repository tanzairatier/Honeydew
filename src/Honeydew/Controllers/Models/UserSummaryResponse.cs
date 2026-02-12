namespace Honeydew.Controllers.Models;

public class UserSummaryResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool CanViewAllTodos { get; set; }
    public bool CanEditAllTodos { get; set; }
    public bool CanCreateUser { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
