using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeydew.Data;

public class UserPreferencesDataAccess(HoneydewDbContext db)
{
    private readonly HoneydewDbContext _db = db;

    public async Task<UserPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<UserPreference> GetOrCreateAsync(Guid userId, int defaultItemsPerPage, CancellationToken cancellationToken = default)
    {
        var p = await GetByUserIdAsync(userId, cancellationToken);
        if (p != null)
        {
            return p;
        }
        p = new UserPreference { UserId = userId, ItemsPerPage = defaultItemsPerPage };
        _db.UserPreferences.Add(p);
        await _db.SaveChangesAsync(cancellationToken);
        return p;
    }

    public async Task<bool> SetItemsPerPageAsync(Guid userId, int itemsPerPage, CancellationToken cancellationToken = default)
    {
        var p = await _db.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (p == null)
        {
            p = new UserPreference { UserId = userId, ItemsPerPage = itemsPerPage };
            _db.UserPreferences.Add(p);
        }
        else
        {
            p.ItemsPerPage = itemsPerPage;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
