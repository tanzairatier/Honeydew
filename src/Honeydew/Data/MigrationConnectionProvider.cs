using System.Data.Common;

namespace Honeydew.Data;

/// <summary>
/// Provides the current database connection to migrations so they can run existence checks
/// and skip idempotently when the schema is already applied. Set before calling MigrateAsync() at runtime.
/// Not set during design-time (dotnet ef), so migrations run normally then.
/// </summary>
public static class MigrationConnectionProvider
{
    private static DbConnection? _connection;

    public static void SetConnection(DbConnection? connection) => _connection = connection;

    public static DbConnection? GetConnection() => _connection;
}
