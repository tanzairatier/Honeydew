namespace Honeydew.Entities;

public class BillingPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Display order and slug: Free, Upgraded, Pro.</summary>
    public string Code { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public decimal PricePerMonth { get; set; }
    /// <summary>0–100. Applied in UI as discount (e.g. 20 = 20% off).</summary>
    public int PromotionPercent { get; set; }
}
