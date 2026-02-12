using Honeydew.Controllers.Models;
using Honeydew.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Honeydew.Controllers;

[ApiController]
[Route("api/billing-plans")]
[Authorize]
public class BillingPlansController(HoneydewDbContext db) : ControllerBase
{
    private readonly HoneydewDbContext _db = db;

    /// <summary>List all billing plans (for plan picker).</summary>
    [HttpGet]
    public async Task<ActionResult<List<BillingPlanResponse>>> List(CancellationToken cancellationToken = default)
    {
        var plans = await _db.BillingPlans
            .AsNoTracking()
            .OrderBy(p => p.MaxUsers)
            .Select(p => new BillingPlanResponse
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                MaxUsers = p.MaxUsers,
                PricePerMonth = p.PricePerMonth,
                PromotionPercent = p.PromotionPercent
            })
            .ToListAsync(cancellationToken);
        return Ok(plans);
    }
}
