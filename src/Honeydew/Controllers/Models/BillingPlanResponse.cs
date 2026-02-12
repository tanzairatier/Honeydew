namespace Honeydew.Controllers.Models;

public class BillingPlanResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public decimal PricePerMonth { get; set; }
    public int PromotionPercent { get; set; }
}
