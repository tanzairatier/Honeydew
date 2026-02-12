using Honeydew.Controllers.Models;
using Honeydew.Data;
using Honeydew.Entities;

namespace Honeydew.Services;

public class UserPreferencesService(UserPreferencesDataAccess data)
{
    private const int DefaultItemsPerPage = 9;
    private static readonly int[] AllowedItemsPerPage = { 9, 12, 15, 18, 21, 24 };

    private readonly UserPreferencesDataAccess _data = data;

    public async Task<UserPreferencesResponse?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var p = await _data.GetOrCreateAsync(userId, DefaultItemsPerPage, cancellationToken);
        return new UserPreferencesResponse { ItemsPerPage = p.ItemsPerPage };
    }

    public async Task<(UserPreferencesResponse? Prefs, string? Error)> SetItemsPerPageAsync(Guid userId, int itemsPerPage, CancellationToken cancellationToken = default)
    {
        if (!AllowedItemsPerPage.Contains(itemsPerPage))
        {
            return (null, "Invalid items per page value.");
        }
        await _data.SetItemsPerPageAsync(userId, itemsPerPage, cancellationToken);
        return (await GetAsync(userId, cancellationToken), null);
    }
}
