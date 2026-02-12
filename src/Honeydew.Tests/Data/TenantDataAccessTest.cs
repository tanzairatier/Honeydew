using Honeydew.Data;
using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Data;

[TestClass]
public class TenantDataAccessTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    [TestMethod]
    public async Task GetActiveByIdAsync_ExistingActive_ReturnsTenant()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T1", CreatedAt = DateTime.UtcNow, IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var sut = new TenantDataAccess(db);

        // Act
        var result = await sut.GetActiveByIdAsync(tenant.Id, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(tenant.Id, result.Id);
        Assert.AreEqual("T1", result.Name);
    }

    [TestMethod]
    public async Task GetActiveByIdAsync_Inactive_ReturnsNull()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T1", CreatedAt = DateTime.UtcNow, IsActive = false };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var sut = new TenantDataAccess(db);

        // Act
        var result = await sut.GetActiveByIdAsync(tenant.Id, CancellationToken.None);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetUserCountAsync_ReturnsCount()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "u@t.com",
            DisplayName = "U",
            PasswordHash = "h",
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sut = new TenantDataAccess(db);

        // Act
        var count = await sut.GetUserCountAsync(tenant.Id, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public async Task UpdateNameAsync_Existing_UpdatesAndReturnsTrue()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Old", CreatedAt = DateTime.UtcNow, IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var sut = new TenantDataAccess(db);

        // Act
        var result = await sut.UpdateNameAsync(tenant.Id, "New Name", CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        await db.Entry(tenant).ReloadAsync();
        Assert.AreEqual("New Name", tenant.Name);
    }

    [TestMethod]
    public async Task SetBillingPlanIdAsync_Existing_SetsPlan()
    {
        // Arrange
        await using var db = CreateDb();
        var plan = new BillingPlan { Id = Guid.NewGuid(), Name = "Free", Code = "Free", MaxUsers = 5, PricePerMonth = 0, PromotionPercent = 0 };
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        db.BillingPlans.Add(plan);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var sut = new TenantDataAccess(db);

        // Act
        var result = await sut.SetBillingPlanIdAsync(tenant.Id, plan.Id, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        await db.Entry(tenant).ReloadAsync();
        Assert.AreEqual(plan.Id, tenant.BillingPlanId);
    }

    [TestMethod]
    public async Task GetBillingPlanByCodeAsync_Existing_ReturnsPlan()
    {
        // Arrange
        await using var db = CreateDb();
        var plan = new BillingPlan { Id = Guid.NewGuid(), Name = "Pro", Code = "Pro", MaxUsers = 100, PricePerMonth = 20, PromotionPercent = 0 };
        db.BillingPlans.Add(plan);
        await db.SaveChangesAsync();
        var sut = new TenantDataAccess(db);

        // Act
        var result = await sut.GetBillingPlanByCodeAsync("Pro", CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Pro", result.Code);
        Assert.AreEqual(100, result.MaxUsers);
    }
}
