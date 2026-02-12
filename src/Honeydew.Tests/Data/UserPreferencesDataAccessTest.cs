using Honeydew.Data;
using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Data;

[TestClass]
public class UserPreferencesDataAccessTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    [TestMethod]
    public async Task GetOrCreateAsync_NoExisting_CreatesAndReturnsDefault()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        var sut = new UserPreferencesDataAccess(db);

        // Act
        var prefs = await sut.GetOrCreateAsync(userId, 12, CancellationToken.None);

        // Assert
        Assert.IsNotNull(prefs);
        Assert.AreEqual(userId, prefs.UserId);
        Assert.AreEqual(12, prefs.ItemsPerPage);
    }

    [TestMethod]
    public async Task SetItemsPerPageAsync_UpdatesValue()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
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
        db.UserPreferences.Add(new UserPreference { UserId = userId, ItemsPerPage = 9 });
        await db.SaveChangesAsync();
        var sut = new UserPreferencesDataAccess(db);

        // Act
        await sut.SetItemsPerPageAsync(userId, 18, CancellationToken.None);

        // Assert
        var prefs = await db.UserPreferences.FindAsync(userId);
        Assert.IsNotNull(prefs);
        Assert.AreEqual(18, prefs.ItemsPerPage);
    }
}
