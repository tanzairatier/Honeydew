using Honeydew.Controllers.Models;
using Honeydew.Data;
using Honeydew.Entities;
using Honeydew.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Services;

[TestClass]
public class UsersServiceTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    private static UsersService CreateService(HoneydewDbContext db)
    {
        return new UsersService(new UsersDataAccess(db));
    }

    [TestMethod]
    public async Task ListUsersAsync_ForAssignmentOnly_AnyMember_ReturnsList()
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
            Email = "member@t.com",
            DisplayName = "Member",
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
        var (list, error) = await sut.ListUsersAsync(userId, tenantId, true, true, CancellationToken.None);

        // Assert
        Assert.IsNull(error);
        Assert.IsNotNull(list);
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("Member", list[0].DisplayName);
    }

    [TestMethod]
    public async Task ListUsersAsync_NotForAssignment_MemberWithoutCanCreateUser_Forbid()
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
            Email = "member@t.com",
            DisplayName = "Member",
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
        var (list, error) = await sut.ListUsersAsync(userId, tenantId, true, false, CancellationToken.None);

        // Assert
        Assert.IsNull(list);
        Assert.AreEqual("Forbid", error);
    }

    [TestMethod]
    public async Task GetCurrentUserAsync_Existing_ReturnsSummary()
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
            DisplayName = "Me",
            PasswordHash = "h",
            Role = UserRole.Owner,
            CanViewAllTodos = true,
            CanEditAllTodos = true,
            CanCreateUser = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        // Act
        var (user, error) = await sut.GetCurrentUserAsync(userId, tenantId, CancellationToken.None);

        // Assert
        Assert.IsNull(error);
        Assert.IsNotNull(user);
        Assert.AreEqual("Me", user.DisplayName);
        Assert.AreEqual("Owner", user.Role);
    }
}
