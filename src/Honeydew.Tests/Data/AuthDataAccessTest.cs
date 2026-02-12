using Honeydew.Data;
using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Data;

[TestClass]
public class AuthDataAccessTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    [TestMethod]
    public async Task AnyUserWithEmailAsync_Existing_ReturnsTrue()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "user@test.com",
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
        var sut = new AuthDataAccess(db);

        // Act
        var result = await sut.AnyUserWithEmailAsync("user@test.com", CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task GetUsersByEmailWithTenantAsync_ReturnsActiveUsersWithTenant()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Acme", CreatedAt = DateTime.UtcNow, IsActive = true };
        db.Tenants.Add(tenant);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "same@test.com",
            DisplayName = "U1",
            PasswordHash = "h",
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = new AuthDataAccess(db);

        // Act
        var list = await sut.GetUsersByEmailWithTenantAsync("same@test.com", CancellationToken.None);

        // Assert
        Assert.AreEqual(1, list.Count);
        Assert.IsNotNull(list[0].Tenant);
        Assert.AreEqual("Acme", list[0].Tenant.Name);
    }

    [TestMethod]
    public async Task AddTenantAndUserAsync_PersistsBoth()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "New", CreatedAt = DateTime.UtcNow, IsActive = true };
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "new@test.com",
            DisplayName = "New User",
            PasswordHash = "h",
            Role = UserRole.Owner,
            CanViewAllTodos = true,
            CanEditAllTodos = true,
            CanCreateUser = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var sut = new AuthDataAccess(db);

        // Act
        await sut.AddTenantAndUserAsync(tenant, user, CancellationToken.None);

        // Assert
        var t = await db.Tenants.FindAsync(tenant.Id);
        var u = await db.Users.FindAsync(user.Id);
        Assert.IsNotNull(t);
        Assert.AreEqual("New", t.Name);
        Assert.IsNotNull(u);
        Assert.AreEqual("new@test.com", u.Email);
    }
}
