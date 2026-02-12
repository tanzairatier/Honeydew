namespace Honeydew.Entities;

/// <summary>
/// Named privileges for household users. Owner has all; members have explicit flags.
/// Used for future user-editing portal and delegation.
/// </summary>
public static class Privilege
{
    public const string CreateTodo = nameof(CreateTodo);
    public const string EditTodo = nameof(EditTodo);
    public const string CreateUser = nameof(CreateUser);

    /// <summary>Owner (household manager) has all rights.</summary>
    public static bool Has(User user, string privilege)
    {
        if (user.Role == UserRole.Owner)
        {
            return true;
        }

        return privilege switch
        {
            CreateTodo => true,
            EditTodo => user.CanEditAllTodos,
            CreateUser => user.CanCreateUser,
            _ => false
        };
    }

    public static bool CanCreateTodo(User user) => Has(user, CreateTodo);
    public static bool CanEditTodo(User user) => Has(user, EditTodo);
    public static bool CanCreateUser(User user) => Has(user, CreateUser);
}
