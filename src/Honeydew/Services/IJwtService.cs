using Honeydew.Entities;

namespace Honeydew.Services;

public interface IJwtService
{
    string CreateUserToken(User user, Tenant tenant);
    string CreateClientToken(ApiClient client, Tenant tenant);
}
