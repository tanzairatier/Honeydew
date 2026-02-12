using Honeydew.Data;
using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Data;

[TestClass]
public class SupportTicketsDataAccessTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    [TestMethod]
    public async Task GetByTenantIdAsync_ReturnsTicketsOrderedByCreatedAtDesc()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        db.SupportTickets.Add(new SupportTicket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Subject = "First",
            Body = "B1",
            Status = "Open",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });
        db.SupportTickets.Add(new SupportTicket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Subject = "Second",
            Body = "B2",
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = new SupportTicketsDataAccess(db);

        // Act
        var list = await sut.GetByTenantIdAsync(tenantId, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual("Second", list[0].Subject);
        Assert.AreEqual("First", list[1].Subject);
    }

    [TestMethod]
    public async Task AddAsync_PersistsTicket()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        await db.SaveChangesAsync();
        var sut = new SupportTicketsDataAccess(db);
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Subject = "Help",
            Body = "I need help",
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var added = await sut.AddAsync(ticket, CancellationToken.None);

        // Assert
        Assert.AreEqual(ticket.Id, added.Id);
        var found = await db.SupportTickets.FindAsync(ticket.Id);
        Assert.IsNotNull(found);
        Assert.AreEqual("Help", found.Subject);
    }

    [TestMethod]
    public async Task AddReplyAsync_PersistsReply()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        db.SupportTickets.Add(new SupportTicket
        {
            Id = ticketId,
            TenantId = tenantId,
            Subject = "S",
            Body = "B",
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = new SupportTicketsDataAccess(db);
        var reply = new SupportTicketReply
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticketId,
            Body = "Reply body",
            IsFromStaff = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var added = await sut.AddReplyAsync(reply, CancellationToken.None);

        // Assert
        Assert.AreEqual(reply.Id, added.Id);
        var found = await db.SupportTicketReplies.FindAsync(reply.Id);
        Assert.IsNotNull(found);
        Assert.AreEqual("Reply body", found.Body);
    }
}
