using Honeydew.Controllers.Models;
using Honeydew.Data;
using Honeydew.Entities;

namespace Honeydew.Services;

public class TodoService(TodoDataAccess data, UsersDataAccess usersData)
{
    private readonly TodoDataAccess _data = data;
    private readonly UsersDataAccess _usersData = usersData;

    public async Task<(List<TodoItemResponse>? List, int TotalCount, string? Error)> GetPageAsync(
        Guid userId,
        Guid tenantId,
        bool onlyMine,
        bool includeCompleted,
        IReadOnlyList<Guid>? assignedToUserIds,
        string? search,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, 0, "Unauthorized");
        }

        var canViewAll = user.Role == UserRole.Owner || user.CanViewAllTodos;
        var onlyMineEffective = onlyMine || !canViewAll;
        var createdBy = onlyMineEffective ? userId : (Guid?)null;

        var (items, totalCount) = await _data.GetPageAsync(
            tenantId, createdBy, assignedToUserIds, onlyMineEffective, includeCompleted, search, sortBy, sortDesc, page, pageSize, cancellationToken);
        var voteCounts = await _data.GetVoteCountsAsync(items.Select(t => t.Id).ToList(), cancellationToken);
        var userVoted = await _data.GetUserVotedTodoIdsAsync(userId, items.Select(t => t.Id).ToList(), cancellationToken);
        var list = items.Select(t => Map(t, voteCounts.GetValueOrDefault(t.Id, 0), userVoted.Contains(t.Id))).ToList();
        return (list, totalCount, null);
    }

    public async Task<(TodoItemResponse? Item, string? Error)> GetByIdAsync(
        Guid userId,
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }

        var item = await _data.GetByIdAsync(id, tenantId, cancellationToken);
        if (item == null)
        {
            return (null, "NotFound");
        }
        var canViewAll = user.Role == UserRole.Owner || user.CanViewAllTodos;
        if (!canViewAll && item.CreatedByUserId != userId)
        {
            return (null, "Forbid");
        }
        var voteCount = (await _data.GetVoteCountsAsync(new[] { id }, cancellationToken)).GetValueOrDefault(id, 0);
        var userVoted = (await _data.GetUserVotedTodoIdsAsync(userId, new[] { id }, cancellationToken)).Contains(id);
        return (Map(item, voteCount, userVoted), null);
    }

    public async Task<(TodoItemResponse? Item, string? Error)> CreateAsync(
        Guid userId,
        Guid tenantId,
        CreateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }
        if (!Privilege.CanCreateTodo(user))
        {
            return (null, "Forbid");
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return (null, "Title is required.");
        }
        var assignedUser = await _usersData.GetByIdAsync(request.AssignedToUserId, cancellationToken);
        if (assignedUser == null || assignedUser.TenantId != tenantId)
        {
            return (null, "AssignedToUserId must be a user in your tenant.");
        }

        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedByUserId = userId,
            AssignedToUserId = request.AssignedToUserId,
            Title = request.Title.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsDone = false,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow
        };
        await _data.AddAsync(item, cancellationToken);
        return (Map(item, 0, false), null);
    }

    public async Task<(TodoItemResponse? Item, string? Error)> UpdateAsync(
        Guid userId,
        Guid tenantId,
        Guid id,
        UpdateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }

        var item = await _data.GetByIdForUpdateAsync(id, tenantId, cancellationToken);
        if (item == null)
        {
            return (null, "NotFound");
        }
        var canEditAll = user.Role == UserRole.Owner || user.CanEditAllTodos;
        var isOwnTodo = item.CreatedByUserId == userId || item.AssignedToUserId == userId;
        if (!canEditAll && !isOwnTodo)
        {
            return (null, "Forbid");
        }

        if (request.Title != null)
        {
            item.Title = request.Title.Trim();
        }
        if (request.Notes != null)
        {
            item.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        }
        if (request.AssignedToUserId.HasValue)
        {
            var assignedUser = await _usersData.GetByIdAsync(request.AssignedToUserId.Value, cancellationToken);
            if (assignedUser == null || assignedUser.TenantId != tenantId)
            {
                return (null, "AssignedToUserId must be a user in your tenant.");
            }
            item.AssignedToUserId = request.AssignedToUserId;
        }
        if (request.DueDate.HasValue)
        {
            item.DueDate = request.DueDate;
        }
        if (request.IsDone.HasValue)
        {
            item.IsDone = request.IsDone.Value;
            item.CompletedAt = request.CompletedAt ?? (request.IsDone.Value ? DateTime.UtcNow : null);
        }
        else if (request.CompletedAt.HasValue)
        {
            item.CompletedAt = request.CompletedAt;
        }

        await _data.SaveChangesAsync(cancellationToken);
        var voteCount = (await _data.GetVoteCountsAsync(new[] { item.Id }, cancellationToken)).GetValueOrDefault(item.Id, 0);
        var userVoted = (await _data.GetUserVotedTodoIdsAsync(userId, new[] { item.Id }, cancellationToken)).Contains(item.Id);
        return (Map(item, voteCount, userVoted), null);
    }

    public async Task<(List<TodoItemResponse>? List, string? Error)> GetAllForExportAsync(
        Guid userId,
        Guid tenantId,
        bool onlyMine,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }
        var canViewAll = user.Role == UserRole.Owner || user.CanViewAllTodos;
        if (!onlyMine && !canViewAll)
        {
            return (null, "Forbid");
        }

        var items = await _data.GetAllForExportAsync(tenantId, onlyMine, userId, cancellationToken);
        return (items.Select(t => Map(t, 0, false)).ToList(), null);
    }

    public async Task<(List<TodoItemResponse>? List, string? Error)> GetTopAssignedToMeAsync(
        Guid userId,
        Guid tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }
        var items = await _data.GetTopAssignedToUserAsync(tenantId, userId, take, cancellationToken);
        var voteCounts = await _data.GetVoteCountsAsync(items.Select(t => t.Id).ToList(), cancellationToken);
        var userVoted = await _data.GetUserVotedTodoIdsAsync(userId, items.Select(t => t.Id).ToList(), cancellationToken);
        var list = items.Select(t => Map(t, voteCounts.GetValueOrDefault(t.Id, 0), userVoted.Contains(t.Id))).ToList();
        return (list, null);
    }

    public async Task<(bool? Voted, string? Error)> ToggleVoteAsync(
        Guid userId,
        Guid tenantId,
        Guid todoId,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }
        var voted = await _data.ToggleVoteAsync(todoId, userId, tenantId, cancellationToken);
        return (voted, null);
    }

    private static TodoItemResponse Map(TodoItem t, int voteCount, bool currentUserVoted)
    {
        return new TodoItemResponse
        {
            Id = t.Id,
            Title = t.Title,
            Notes = t.Notes,
            IsDone = t.IsDone,
            CompletedAt = t.CompletedAt,
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt,
            CreatedByUserId = t.CreatedByUserId,
            AssignedToUserId = t.AssignedToUserId,
            VoteCount = voteCount,
            CurrentUserVoted = currentUserVoted
        };
    }
}
