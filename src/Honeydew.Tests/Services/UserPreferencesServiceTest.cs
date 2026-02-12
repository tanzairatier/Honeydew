using Honeydew.Data;
using Honeydew.Entities;
using Honeydew.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Services;

[TestClass]
public class UserPreferencesServiceTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    private static UserPreferencesService CreateService(HoneydewDbContext db)
    {
        return new UserPreferencesService(new UserPreferencesDataAccess(db));
    }

    [TestMethod]
    public async Task GetAsync_NoExisting_CreatesDefaultAndReturns()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        db.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = "u@t.com",
            DisplayName = "U",
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
        var prefs = await sut.GetAsync(userId, CancellationToken.None);

        // Assert
        Assert.IsNotNull(prefs);
        Assert.AreEqual(9, prefs.ItemsPerPage);
    }

    [TestMethod]
    public async Task SetItemsPerPageAsync_ValidValue_UpdatesAndReturns()
    {
        // Arrange
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        db.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = "u@t.com",
            DisplayName = "U",
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
        var (prefs, error) = await sut.SetItemsPerPageAsync(userId, 18, CancellationToken.None);

        // Assert
        Assert.IsNull(error);
        Assert.IsNotNull(prefs);
        Assert.AreEqual(18, prefs.ItemsPerPage);
    }

    [TestMethod]
    public async Task SetItemsPerPageAsync_InvalidValue_ReturnsError()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = CreateService(db);

        // Act
        var (prefs, error) = await sut.SetItemsPerPageAsync(Guid.NewGuid(), 99, CancellationToken.None);

        // Assert
        Assert.IsNull(prefs);
        Assert.IsNotNull(error);
    }
}
