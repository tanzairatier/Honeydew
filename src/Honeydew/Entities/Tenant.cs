namespace Honeydew.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SettingsJson { get; set; }
    public Guid? BillingPlanId { get; set; }

    public BillingPlan? BillingPlan { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<ApiClient> ApiClients { get; set; } = new List<ApiClient>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
}
