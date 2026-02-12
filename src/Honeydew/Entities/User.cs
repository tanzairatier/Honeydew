namespace Honeydew.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordSalt { get; set; }
    public UserRole Role { get; set; }
    public bool CanViewAllTodos { get; set; }
    public bool CanEditAllTodos { get; set; }
    public bool CanCreateUser { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
