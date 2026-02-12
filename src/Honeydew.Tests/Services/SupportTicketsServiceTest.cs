using Honeydew.Controllers.Models;
using Honeydew.Data;
using Honeydew.Entities;
using Honeydew.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Services;

[TestClass]
public class SupportTicketsServiceTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    private static SupportTicketsService CreateService(HoneydewDbContext db)
    {
        return new SupportTicketsService(new SupportTicketsDataAccess(db), new UsersDataAccess(db));
    }

    [TestMethod]
    public async Task CreateAsync_ValidInput_CreatesTicket()
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
        var sut = CreateService(db);
        var request = new CreateSupportTicketRequest { Subject = "Help", Body = "I need help" };

        // Act
        var (ticket, error) = await sut.CreateAsync(userId, tenantId, request, CancellationToken.None);

        // Assert
        Assert.IsNull(error);
        Assert.IsNotNull(ticket);
        Assert.AreEqual("Help", ticket.Subject);
        Assert.AreEqual("Open", ticket.Status);
    }

    [TestMethod]
    public async Task CreateAsync_EmptySubject_ReturnsError()
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
        var sut = CreateService(db);
        var request = new CreateSupportTicketRequest { Subject = "  ", Body = "B" };

        // Act
        var (ticket, error) = await sut.CreateAsync(userId, tenantId, request, CancellationToken.None);

        // Assert
        Assert.IsNull(ticket);
        Assert.IsNotNull(error);
        Assert.IsTrue(error.Contains("Subject", StringComparison.OrdinalIgnoreCase));
    }
}
