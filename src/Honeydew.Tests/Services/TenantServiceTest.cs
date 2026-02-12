using Honeydew.Data;
using Honeydew.Entities;
using Honeydew.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Services;

[TestClass]
public class TenantServiceTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    private static TenantService CreateService(HoneydewDbContext db)
    {
        return new TenantService(new TenantDataAccess(db));
    }

    [TestMethod]
    public async Task GetTenantAsync_NotFound_ReturnsNull()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = CreateService(db);

        // Act
        var result = await sut.GetTenantAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetTenantAsync_Found_ReturnsResponseWithUserCount()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Acme", CreatedAt = DateTime.UtcNow, IsActive = true };
        db.Tenants.Add(tenant);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "u@a.com",
            DisplayName = "U",
            PasswordHash = "h",
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "u2@a.com",
            DisplayName = "U2",
            PasswordHash = "h",
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        // Act
        var result = await sut.GetTenantAsync(tenant.Id, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(tenant.Id, result.Id);
        Assert.AreEqual("Acme", result.Name);
        Assert.AreEqual(2, result.UserCount);
    }

    [TestMethod]
    public async Task UpdateNameAsync_EmptyName_ReturnsFalse()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = CreateService(db);

        // Act
        var result = await sut.UpdateNameAsync(Guid.NewGuid(), "  ", CancellationToken.None);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task UpdateNameAsync_ValidName_UpdatesAndReturnsTrue()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Old", CreatedAt = DateTime.UtcNow, IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        // Act
        var result = await sut.UpdateNameAsync(tenant.Id, "New Name", CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        await db.Entry(tenant).ReloadAsync();
        Assert.AreEqual("New Name", tenant.Name);
    }

    [TestMethod]
    public async Task UpdateBillingPlanAsync_Valid_SetsPlanAndReturnsTrue()
    {
        // Arrange
        await using var db = CreateDb();
        var plan = new BillingPlan { Id = Guid.NewGuid(), Name = "Pro", Code = "Pro", MaxUsers = 100, PricePerMonth = 20, PromotionPercent = 0 };
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        db.BillingPlans.Add(plan);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        // Act
        var result = await sut.UpdateBillingPlanAsync(tenant.Id, plan.Id, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        await db.Entry(tenant).ReloadAsync();
        Assert.AreEqual(plan.Id, tenant.BillingPlanId);
    }
}
