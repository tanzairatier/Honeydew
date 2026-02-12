using Honeydew.Entities;
using Honeydew.Services;
using Microsoft.EntityFrameworkCore;

namespace Honeydew.Data;

public static class SeedData
{
    /// <summary>Seed one tenant, owner user, and API client for local testing when DB is empty.</summary>
    public static async Task SeedDevIfEmpty(HoneydewDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        var (hash, salt) = PasswordHasher.HashPassword("TestPassword1!");
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Default Household",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "test@example.com",
            DisplayName = "Test Owner",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Owner,
            CanViewAllTodos = true,
            CanEditAllTodos = true,
            CanCreateUser = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        var clientSecretHash = ClientSecretHasher.Hash("client-secret", "client-id");
        var apiClient = new ApiClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ClientId = "client-id",
            ClientSecretHash = clientSecretHash,
            Name = "Postman / API",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Tenants.Add(tenant);
        db.Users.Add(user);
        db.ApiClients.Add(apiClient);
        await db.SaveChangesAsync(cancellationToken);

        await SeedBillingPlansIfEmptyAsync(db, cancellationToken);
    }

    /// <summary>Seed billing plans when empty. Optionally set default tenant to Free plan.</summary>
    public static async Task SeedBillingPlansIfEmptyAsync(HoneydewDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.BillingPlans.AnyAsync(cancellationToken))
        {
            return;
        }

        var free = new BillingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Free",
            Code = "Free",
            MaxUsers = 3,
            PricePerMonth = 0,
            PromotionPercent = 0
        };
        var upgraded = new BillingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Upgraded",
            Code = "Upgraded",
            MaxUsers = 10,
            PricePerMonth = 9.99m,
            PromotionPercent = 0
        };
        var pro = new BillingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Pro",
            Code = "Pro",
            MaxUsers = 100,
            PricePerMonth = 19.99m,
            PromotionPercent = 0
        };
        db.BillingPlans.AddRange(free, upgraded, pro);
        await db.SaveChangesAsync(cancellationToken);

        var defaultTenant = await db.Tenants.FirstOrDefaultAsync(cancellationToken);
        if (defaultTenant != null)
        {
            defaultTenant.BillingPlanId = free.Id;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
