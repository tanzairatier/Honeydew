using Honeydew.Data;
using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Data;

[TestClass]
public class TodoDataAccessTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    [TestMethod]
    public async Task GetPageAsync_ReturnsItemsAndTotalCount()
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
        db.TodoItems.Add(new TodoItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedByUserId = userId,
            Title = "Todo 1",
            IsDone = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = new TodoDataAccess(db);

        // Act
        var (items, total) = await sut.GetPageAsync(
            tenantId, null, null, false, true, null, "createdat", true, 1, 10, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, total);
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("Todo 1", items[0].Title);
    }

    [TestMethod]
    public async Task GetByIdAsync_Existing_ReturnsItem()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        var item = new TodoItem
        {
            Id = itemId,
            TenantId = tenantId,
            CreatedByUserId = Guid.NewGuid(),
            Title = "Find me",
            IsDone = false,
            CreatedAt = DateTime.UtcNow
        };
        db.TodoItems.Add(item);
        await db.SaveChangesAsync();
        var sut = new TodoDataAccess(db);

        // Act
        var result = await sut.GetByIdAsync(itemId, tenantId, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Find me", result.Title);
    }

    [TestMethod]
    public async Task AddAsync_PersistsItem()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        await db.SaveChangesAsync();
        var sut = new TodoDataAccess(db);
        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedByUserId = Guid.NewGuid(),
            Title = "New",
            IsDone = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var added = await sut.AddAsync(item, CancellationToken.None);

        // Assert
        Assert.AreEqual(item.Id, added.Id);
        var found = await db.TodoItems.FindAsync(item.Id);
        Assert.IsNotNull(found);
        Assert.AreEqual("New", found.Title);
    }

    [TestMethod]
    public async Task ToggleVoteAsync_NoExistingVote_AddsVoteAndReturnsTrue()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
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
        db.TodoItems.Add(new TodoItem
        {
            Id = todoId,
            TenantId = tenantId,
            CreatedByUserId = userId,
            Title = "Todo",
            IsDone = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = new TodoDataAccess(db);

        // Act
        var result = await sut.ToggleVoteAsync(todoId, userId, tenantId, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        var vote = await db.TodoItemVotes.FirstOrDefaultAsync(v => v.TodoItemId == todoId && v.UserId == userId);
        Assert.IsNotNull(vote);
    }
}
