using Honeydew.Controllers.Models;
using Honeydew.Data;
using Honeydew.Entities;

namespace Honeydew.Services;

public class UsersService(UsersDataAccess data)
{
    private readonly UsersDataAccess _data = data;

    public async Task<(List<UserSummaryResponse>? List, string? Error)> ListUsersAsync(
        Guid userId,
        Guid tenantId,
        bool activeOnly,
        bool forAssignmentOnly = false,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _data.GetByIdAsync(userId, cancellationToken);
        if (currentUser == null || currentUser.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }
        if (!forAssignmentOnly && currentUser.Role != UserRole.Owner && !currentUser.CanCreateUser)
        {
            return (null, "Forbid");
        }

        var users = await _data.GetByTenantIdAsync(tenantId, activeOnly, cancellationToken);
        var list = users.Select(MapToSummary).ToList();
        return (list, null);
    }

    public async Task<(UserSummaryResponse? User, string? Error)> GetCurrentUserAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var user = await _data.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }
        return (MapToSummary(user), null);
    }

    public async Task<(UserSummaryResponse? User, string? Error)> CreateUserAsync(
        Guid userId,
        Guid tenantId,
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _data.GetByIdAsync(userId, cancellationToken);
        if (currentUser == null || currentUser.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }
        if (!Privilege.CanCreateUser(currentUser))
        {
            return (null, "Forbid");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(request.Password))
        {
            return (null, "Email and password are required.");
        }

        if (await _data.ExistsEmailInTenantAsync(tenantId, email, cancellationToken))
        {
            return (null, "A user with this email already exists in this household.");
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            role = UserRole.Member;
        }

        var (hash, salt) = PasswordHasher.HashPassword(request.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            CanViewAllTodos = request.CanViewAllTodos,
            CanEditAllTodos = request.CanEditAllTodos,
            CanCreateUser = request.CanCreateUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _data.AddUserAsync(user, cancellationToken);
        return (MapToSummary(user), null);
    }

    public async Task<(UserSummaryResponse? User, string? Error)> UpdateUserAsync(
        Guid userId,
        Guid tenantId,
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _data.GetByIdAsync(userId, cancellationToken);
        if (currentUser == null || currentUser.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }

        var user = await _data.GetByIdAndTenantIdForUpdateAsync(id, tenantId, cancellationToken);
        if (user == null)
        {
            return (null, "NotFound");
        }

        var isOwner = currentUser.Role == UserRole.Owner;
        var isSelf = user.Id == userId;

        if (request.DisplayName != null)
        {
            user.DisplayName = request.DisplayName.Trim();
        }

        if (isOwner && !isSelf)
        {
            if (request.Role != null && Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                user.Role = role;
            }
            if (request.CanViewAllTodos.HasValue)
            {
                user.CanViewAllTodos = request.CanViewAllTodos.Value;
            }
            if (request.CanEditAllTodos.HasValue)
            {
                user.CanEditAllTodos = request.CanEditAllTodos.Value;
            }
            if (request.CanCreateUser.HasValue)
            {
                user.CanCreateUser = request.CanCreateUser.Value;
            }
            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }
        }

        await _data.SaveChangesAsync(cancellationToken);
        return (MapToSummary(user), null);
    }

    public async Task<(UserSummaryResponse? User, string? Error)> UpdateCurrentUserAsync(
        Guid userId,
        Guid tenantId,
        UpdateCurrentUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _data.GetByIdAndTenantIdForUpdateAsync(userId, tenantId, cancellationToken);
        if (user == null)
        {
            return (null, "Unauthorized");
        }

        if (request.DisplayName != null)
        {
            user.DisplayName = request.DisplayName.Trim();
        }

        if (request.Email != null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email))
            {
                return (null, "Email cannot be empty.");
            }
            if (await _data.ExistsEmailInTenantExcludingUserAsync(tenantId, email, userId, cancellationToken))
            {
                return (null, "A user with this email already exists in this household.");
            }
            user.Email = email;
        }

        await _data.SaveChangesAsync(cancellationToken);
        return (MapToSummary(user), null);
    }

    public async Task<(bool Ok, string? Error)> DeleteUserAsync(
        Guid currentUserId,
        Guid tenantId,
        Guid userIdToDelete,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _data.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null || currentUser.TenantId != tenantId)
        {
            return (false, "Unauthorized");
        }

        var target = await _data.GetByIdAndTenantIdAsync(userIdToDelete, tenantId, cancellationToken);
        if (target == null)
        {
            return (false, "NotFound");
        }

        var isOwner = currentUser.Role == UserRole.Owner;
        var isSelf = target.Id == currentUserId;
        if (!isOwner && !isSelf)
        {
            return (false, "Forbid");
        }

        if (target.Role == UserRole.Owner)
        {
            var ownerCount = await _data.CountOwnersInTenantAsync(tenantId, cancellationToken);
            if (ownerCount <= 1)
            {
                return (false, "Cannot delete the last owner.");
            }
        }

        var deleted = await _data.DeleteUserAsync(userIdToDelete, tenantId, cancellationToken);
        return deleted ? (true, null) : (false, "NotFound");
    }

    private static UserSummaryResponse MapToSummary(User u)
    {
        return new UserSummaryResponse
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            Role = u.Role.ToString(),
            CanViewAllTodos = u.CanViewAllTodos,
            CanEditAllTodos = u.CanEditAllTodos,
            CanCreateUser = u.CanCreateUser,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }
}
