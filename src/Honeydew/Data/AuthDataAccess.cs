using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeydew.Data;

public class AuthDataAccess(HoneydewDbContext db)
{
    private readonly HoneydewDbContext _db = db;

    public async Task<bool> AnyUserWithEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<List<User>> GetUsersByEmailWithTenantAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Include(u => u.Tenant)
            .Where(u => u.Email == email && u.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task AddTenantAndUserAsync(Tenant tenant, User user, CancellationToken cancellationToken = default)
    {
        _db.Tenants.Add(tenant);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ApiClient?> GetApiClientByClientIdWithTenantAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _db.ApiClients
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsActive, cancellationToken);
    }

    public async Task<bool> AnyApiClientWithClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _db.ApiClients
            .AsNoTracking()
            .AnyAsync(c => c.ClientId == clientId, cancellationToken);
    }

    public async Task AddApiClientAsync(ApiClient apiClient, CancellationToken cancellationToken = default)
    {
        _db.ApiClients.Add(apiClient);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
