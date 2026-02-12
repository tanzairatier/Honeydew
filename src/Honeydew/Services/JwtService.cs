using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Honeydew.Entities;
using Honeydew.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Honeydew.Services;

public class JwtService(IOptions<JwtOptions> options) : IJwtService
{
    private readonly JwtOptions _options = options.Value;
    private readonly byte[] _keyBytes = Encoding.UTF8.GetBytes(options.Value.SigningKey);

    public string CreateUserToken(User user, Tenant tenant)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("tenant", tenant.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("can_view_all_todos", user.CanViewAllTodos ? "true" : "false"),
            new("can_edit_all_todos", user.CanEditAllTodos ? "true" : "false"),
            new("can_create_user", user.CanCreateUser ? "true" : "false")
        };

        return CreateToken(claims);
    }

    public string CreateClientToken(ApiClient client, Tenant tenant)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, client.Id.ToString()),
            new("client_id", client.ClientId),
            new("tenant", tenant.Id.ToString())
        };

        return CreateToken(claims);
    }

    private string CreateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(_keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
