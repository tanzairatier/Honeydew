using Honeydew.Data;
using Honeydew.Entities;
using Honeydew.Services.Models;

namespace Honeydew.Services;

public class AuthService(AuthDataAccess authData, TenantDataAccess tenantData, IJwtService jwt)
{
    private readonly AuthDataAccess _authData = authData;
    private readonly TenantDataAccess _tenantData = tenantData;
    private readonly IJwtService _jwt = jwt;

    public async Task<AuthResult> RegisterTenant(
        string tenantName,
        string ownerEmail,
        string password,
        string ownerDisplayName,
        CancellationToken cancellationToken = default)
    {
        ownerEmail = ownerEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(ownerEmail) || string.IsNullOrEmpty(password))
        {
            return new AuthResult(false, null, "Email and password are required.");
        }

        if (await _authData.AnyUserWithEmailAsync(ownerEmail, cancellationToken))
        {
            return new AuthResult(false, null, "A user with this email already exists.");
        }

        var (hash, salt) = PasswordHasher.HashPassword(password);
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = tenantName.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = ownerEmail,
            DisplayName = ownerDisplayName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Owner,
            CanViewAllTodos = true,
            CanEditAllTodos = true,
            CanCreateUser = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _authData.AddTenantAndUserAsync(tenant, user, cancellationToken);
        var token = _jwt.CreateUserToken(user, tenant);
        return new AuthResult(true, token, null);
    }

    public async Task<AuthResult> Login(
        string email,
        string password,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        email = email.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return new AuthResult(false, null, "Email and password are required.");
        }

        var users = await _authData.GetUsersByEmailWithTenantAsync(email, cancellationToken);
        if (users.Count == 0)
        {
            return new AuthResult(false, null, "Invalid email or password.");
        }

        User? user;
        if (users.Count > 1)
        {
            if (tenantId == null)
            {
                return new AuthResult(false, null, "User belongs to multiple tenants; include tenantId.");
            }
            user = users.FirstOrDefault(u => u.TenantId == tenantId);
            if (user == null)
            {
                return new AuthResult(false, null, "Invalid email or password.");
            }
        }
        else
        {
            user = users[0];
            if (tenantId != null && user.TenantId != tenantId)
            {
                return new AuthResult(false, null, "Invalid email or password.");
            }
        }

        if (!PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            return new AuthResult(false, null, "Invalid email or password.");
        }

        await _authData.UpdateUserLastLoginAsync(user.Id, cancellationToken);
        var token = _jwt.CreateUserToken(user, user.Tenant);
        return new AuthResult(true, token, null);
    }

    public async Task<AuthResult> GetClientToken(
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        clientId = clientId.Trim();
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return new AuthResult(false, null, "ClientId and ClientSecret are required.");
        }

        var client = await _authData.GetApiClientByClientIdWithTenantAsync(clientId, cancellationToken);
        if (client == null)
        {
            return new AuthResult(false, null, "Invalid client credentials.");
        }
        if (!ClientSecretHasher.Verify(clientSecret, client.ClientId, client.ClientSecretHash))
        {
            return new AuthResult(false, null, "Invalid client credentials.");
        }

        var token = _jwt.CreateClientToken(client, client.Tenant);
        return new AuthResult(true, token, null);
    }

    public async Task<(bool Success, string? ClientId, string? Error)> CreateApiClient(
        Guid tenantId,
        string name,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        clientId = clientId.Trim();
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return (false, null, "ClientId and ClientSecret are required.");
        }

        if (await _authData.AnyApiClientWithClientIdAsync(clientId, cancellationToken))
        {
            return (false, null, "A client with this ClientId already exists.");
        }

        var tenant = await _tenantData.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return (false, null, "Tenant not found.");
        }

        var hash = ClientSecretHasher.Hash(clientSecret, clientId);
        var apiClient = new ApiClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecretHash = hash,
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _authData.AddApiClientAsync(apiClient, cancellationToken);
        return (true, apiClient.ClientId, null);
    }
}
