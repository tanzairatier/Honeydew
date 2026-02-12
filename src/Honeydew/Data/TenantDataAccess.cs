using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeydew.Data;

public class TenantDataAccess(HoneydewDbContext db)
{
    private readonly HoneydewDbContext _db = db;

    public async Task<Tenant?> GetActiveByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.Tenants
            .AsNoTracking()
            .Include(t => t.BillingPlan)
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.Tenants.FindAsync([tenantId], cancellationToken);
    }

    public async Task<int> GetUserCountAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.Users.CountAsync(u => u.TenantId == tenantId, cancellationToken);
    }

    public async Task<BillingPlan?> GetBillingPlanByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _db.BillingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<bool> UpdateNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FindAsync([tenantId], cancellationToken);
        if (tenant == null)
        {
            return false;
        }
        tenant.Name = name.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetBillingPlanIdAsync(Guid tenantId, Guid? billingPlanId, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FindAsync([tenantId], cancellationToken);
        if (tenant == null)
        {
            return false;
        }
        tenant.BillingPlanId = billingPlanId;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
