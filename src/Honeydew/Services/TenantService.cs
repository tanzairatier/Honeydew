using Honeydew.Controllers.Models;
using Honeydew.Data;
using Honeydew.Entities;

namespace Honeydew.Services;

public class TenantService(TenantDataAccess data)
{
    private readonly TenantDataAccess _data = data;

    public async Task<TenantResponse?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _data.GetActiveByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return null;
        }
        var userCount = await _data.GetUserCountAsync(tenantId, cancellationToken);
        var plan = tenant.BillingPlan;
        if (plan == null)
        {
            var freePlan = await _data.GetBillingPlanByCodeAsync("Free", cancellationToken);
            if (freePlan != null)
            {
                plan = freePlan;
            }
        }

        return new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            CreatedAt = tenant.CreatedAt,
            BillingPlanId = tenant.BillingPlanId,
            BillingPlanName = plan?.Name,
            BillingPlanCode = plan?.Code,
            BillingPlanMaxUsers = plan?.MaxUsers,
            UserCount = userCount
        };
    }

    public async Task<bool> UpdateNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }
        return await _data.UpdateNameAsync(tenantId, name.Trim(), cancellationToken);
    }

    public async Task<bool> UpdateBillingPlanAsync(Guid tenantId, Guid? billingPlanId, CancellationToken cancellationToken = default)
    {
        return await _data.SetBillingPlanIdAsync(tenantId, billingPlanId, cancellationToken);
    }
}
