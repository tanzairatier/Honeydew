using Honeydew.Controllers.Models;
using Honeydew.Data;
using Honeydew.Entities;
using Honeydew.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Honeydew.Tests.Services;

[TestClass]
public class TodoServiceTest
{
    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HoneydewDbContext(options);
    }

    private static TodoService CreateService(HoneydewDbContext db)
    {
        return new TodoService(new TodoDataAccess(db), new UsersDataAccess(db));
    }

    [TestMethod]
    public async Task UpdateAsync_OwnTodo_AsMember_UpdatesSuccessfully()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
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
            Title = "Original",
            IsDone = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        // Act
        var (updated, error) = await sut.UpdateAsync(userId, tenantId, todoId, new UpdateTodoRequest { Title = "Updated" }, CancellationToken.None);

        // Assert
        Assert.IsNull(error);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated", updated.Title);
    }

    [TestMethod]
    public async Task UpdateAsync_AssignedToUser_AsMember_UpdatesSuccessfully()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        db.Users.Add(new User
        {
            Id = creatorId,
            TenantId = tenantId,
            Email = "c@t.com",
            DisplayName = "Creator",
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
            Id = assigneeId,
            TenantId = tenantId,
            Email = "a@t.com",
            DisplayName = "Assignee",
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
            CreatedByUserId = creatorId,
            AssignedToUserId = assigneeId,
            Title = "Todo",
            IsDone = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        // Act - assignee (without CanEditAllTodos) can edit because they're assigned
        var (updated, error) = await sut.UpdateAsync(assigneeId, tenantId, todoId, new UpdateTodoRequest { Title = "Done by assignee" }, CancellationToken.None);

        // Assert
        Assert.IsNull(error);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Done by assignee", updated.Title);
    }

    [TestMethod]
    public async Task UpdateAsync_OtherUserTodo_AsMemberWithoutCanEdit_Forbid()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true });
        db.Users.Add(new User
        {
            Id = ownerId,
            TenantId = tenantId,
            Email = "o@t.com",
            DisplayName = "Owner",
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
            Id = otherId,
            TenantId = tenantId,
            Email = "x@t.com",
            DisplayName = "Other",
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
            CreatedByUserId = ownerId,
            AssignedToUserId = ownerId,
            Title = "Owner todo",
            IsDone = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = CreateService(db);

        // Act
        var (updated, error) = await sut.UpdateAsync(otherId, tenantId, todoId, new UpdateTodoRequest { Title = "Hacked" }, CancellationToken.None);

        // Assert
        Assert.IsNull(updated);
        Assert.AreEqual("Forbid", error);
    }
}
