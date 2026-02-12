using System.Data.Common;

namespace Honeydew.Data;

/// <summary>
/// Runs existence checks for idempotent migrations. Each migration id is mapped to SQL that returns a row
/// when the migration's schema is already present (so the migration can skip and still be recorded).
/// </summary>
public static class IdempotentMigrationHelper
{
    private static readonly Dictionary<string, string> CheckSql = new()
    {
        ["00000000000100_InitialCreate"] = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='Tenants' LIMIT 1",
        ["00000000000200_SyncModel"] = "SELECT 1 FROM pragma_table_info('Users') WHERE name='Role' LIMIT 1",
        ["00000000000300_AddCanCreateUser"] = "SELECT 1 FROM pragma_table_info('Users') WHERE name='CanCreateUser' LIMIT 1",
        ["00000000000400_AddTodo"] = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='TodoItems' LIMIT 1",
["00000000000500_AddTodoAssignedDueDateAndVotes"] = "SELECT 1 FROM pragma_table_info('TodoItems') WHERE name='AssignedToUserId' LIMIT 1",
        ["00000000000600_AddBillingAndSupportTickets"] = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='BillingPlans' LIMIT 1",
        ["00000000000700_AddSupportTicketReplies"] = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='SupportTicketReplies' LIMIT 1",
        ["00000000000800_AddUserPreferences"] = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='UserPreferences' LIMIT 1"
    };

    /// <summary>
    /// Returns true if the migration's schema is already applied, so Up() can skip. When connection is null (e.g. design-time), returns false so the migration runs.
    /// </summary>
    public static bool IsAlreadyApplied(string migrationId)
    {
        var conn = MigrationConnectionProvider.GetConnection();
        if (conn == null)
        {
            return false;
        }

        if (!CheckSql.TryGetValue(migrationId, out var sql))
        {
            return false;
        }

        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
        {
            conn.Open();
        }

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value;
        }
        finally
        {
            if (!wasOpen)
            {
                conn.Close();
            }
        }
    }
}
