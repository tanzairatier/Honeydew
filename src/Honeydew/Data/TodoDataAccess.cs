using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeydew.Data;

public class TodoDataAccess(HoneydewDbContext db)
{
    private readonly HoneydewDbContext _db = db;

    public async Task<(List<TodoItem> Items, int TotalCount)> GetPageAsync(
        Guid tenantId,
        Guid? createdByUserId,
        IReadOnlyList<Guid>? assignedToUserIds,
        bool onlyMine,
        bool includeCompleted,
        string? search,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.TodoItems.AsNoTracking().Where(t => t.TenantId == tenantId);

        if (onlyMine && createdByUserId.HasValue)
        {
            query = query.Where(t => t.CreatedByUserId == createdByUserId.Value);
        }
        if (assignedToUserIds != null && assignedToUserIds.Count > 0)
        {
            query = query.Where(t => t.AssignedToUserId != null && assignedToUserIds.Contains(t.AssignedToUserId.Value));
        }
        if (!includeCompleted)
        {
            query = query.Where(t => !t.IsDone);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(term) || (t.Notes != null && t.Notes.ToLower().Contains(term)));
        }

        query = ApplySort(query, sortBy, sortDesc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    private static IQueryable<TodoItem> ApplySort(IQueryable<TodoItem> query, string? sortBy, bool sortDesc)
    {
        return (sortBy?.ToLowerInvariant()) switch
        {
            "duedate" => sortDesc ? query.OrderByDescending(t => t.DueDate == null).ThenByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate),
            "completedat" or "donedate" => sortDesc ? query.OrderByDescending(t => t.CompletedAt) : query.OrderBy(t => t.CompletedAt),
            "createdat" or _ => sortDesc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt)
        };
    }

    public async Task<List<TodoItem>> GetTopAssignedToUserAsync(
        Guid tenantId,
        Guid userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _db.TodoItems.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.AssignedToUserId == userId && !t.IsDone)
            .OrderBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TodoItem>> GetAllForExportAsync(
        Guid tenantId,
        bool onlyMine,
        Guid? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.TodoItems.AsNoTracking().Where(t => t.TenantId == tenantId);
        if (onlyMine && createdByUserId.HasValue)
        {
            query = query.Where(t => t.CreatedByUserId == createdByUserId.Value);
        }
        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<TodoItem?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, cancellationToken);
    }

    public async Task<TodoItem?> GetByIdForUpdateAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, cancellationToken);
    }

    public async Task<TodoItem> AddAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetVoteCountsAsync(IReadOnlyList<Guid> todoItemIds, CancellationToken cancellationToken = default)
    {
        if (todoItemIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }
        var counts = await _db.TodoItemVotes.AsNoTracking()
            .Where(v => todoItemIds.Contains(v.TodoItemId))
            .GroupBy(v => v.TodoItemId)
            .Select(g => new { TodoItemId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return counts.ToDictionary(x => x.TodoItemId, x => x.Count);
    }

    public async Task<HashSet<Guid>> GetUserVotedTodoIdsAsync(Guid userId, IReadOnlyList<Guid> todoItemIds, CancellationToken cancellationToken = default)
    {
        if (todoItemIds.Count == 0)
        {
            return new HashSet<Guid>();
        }
        var voted = await _db.TodoItemVotes.AsNoTracking()
            .Where(v => v.UserId == userId && todoItemIds.Contains(v.TodoItemId))
            .Select(v => v.TodoItemId)
            .ToListAsync(cancellationToken);
        return voted.ToHashSet();
    }

    public async Task<bool> ToggleVoteAsync(Guid todoItemId, Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var todo = await _db.TodoItems.FirstOrDefaultAsync(t => t.Id == todoItemId && t.TenantId == tenantId, cancellationToken);
        if (todo == null)
        {
            return false;
        }
        var existing = await _db.TodoItemVotes.FirstOrDefaultAsync(v => v.TodoItemId == todoItemId && v.UserId == userId, cancellationToken);
        if (existing != null)
        {
            _db.TodoItemVotes.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
            return false; // vote removed
        }
        _db.TodoItemVotes.Add(new TodoItemVote { TodoItemId = todoItemId, UserId = userId });
        await _db.SaveChangesAsync(cancellationToken);
        return true; // vote added
    }
}
