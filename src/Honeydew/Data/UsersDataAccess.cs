using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeydew.Data;

public class UsersDataAccess(HoneydewDbContext db)
{
    private readonly HoneydewDbContext _db = db;

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Users.FindAsync([userId], cancellationToken);
    }

    public async Task<List<User>> GetByTenantIdAsync(Guid tenantId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsNoTracking().Where(u => u.TenantId == tenantId);
        if (activeOnly)
        {
            query = query.Where(u => u.IsActive);
        }
        return await query.OrderBy(u => u.DisplayName).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsEmailInTenantAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsEmailInTenantExcludingUserAsync(Guid tenantId, string email, Guid excludeUserId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.TenantId == tenantId && u.Email == email && u.Id != excludeUserId, cancellationToken);
    }

    public async Task<User?> GetByIdAndTenantIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);
    }

    public async Task<User?> GetByIdAndTenantIdForUpdateAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, cancellationToken);
    }

    public async Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);
        if (user == null)
        {
            return false;
        }
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> CountOwnersInTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.Users.CountAsync(u => u.TenantId == tenantId && u.Role == UserRole.Owner, cancellationToken);
    }
}
