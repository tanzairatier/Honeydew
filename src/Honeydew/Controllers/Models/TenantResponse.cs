namespace Honeydew.Controllers.Models;

public class TenantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid? BillingPlanId { get; set; }
    public string? BillingPlanName { get; set; }
    public string? BillingPlanCode { get; set; }
    public int? BillingPlanMaxUsers { get; set; }
    public int UserCount { get; set; }
}
