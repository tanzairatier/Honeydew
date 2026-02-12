using System.Security.Claims;
using Honeydew.Controllers.Models;
using Honeydew.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Honeydew.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    private readonly AuthService _authService = authService;

    /// <summary>Register a new tenant and its owner user. Returns JWT.</summary>
    [HttpPost("register-tenant")]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _authService.RegisterTenant(
            request.TenantName,
            request.OwnerEmail,
            request.Password,
            request.OwnerDisplayName,
            cancellationToken);

        if (result.Success)
        {
            return Ok(new { Token = result.Token });
        }
        return BadRequest(ApiErrorResponse.From(result.Error ?? "Sign up failed"));
    }

    /// <summary>Authenticate with email and password. Include tenantId when user belongs to multiple tenants.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _authService.Login(
            request.Email,
            request.Password,
            request.TenantId,
            cancellationToken);

        if (result.Success)
        {
            return Ok(new { Token = result.Token });
        }
        if (result.Error?.Contains("multiple tenants") == true)
        {
            return BadRequest(ApiErrorResponse.From(result.Error ?? "Bad request"));
        }
        return Unauthorized(ApiErrorResponse.From(result.Error ?? "Unauthorized"));
    }

    /// <summary>Get JWT for API/client access using client credentials.</summary>
    [HttpPost("token")]
    public async Task<IActionResult> ClientToken([FromBody] ClientTokenRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _authService.GetClientToken(
            request.ClientId,
            request.ClientSecret,
            cancellationToken);

        if (result.Success)
        {
            return Ok(new { Token = result.Token });
        }
        return Unauthorized(ApiErrorResponse.From(result.Error ?? "Invalid credentials"));
    }

    /// <summary>Create an API client for the current tenant. Requires user JWT.</summary>
    [Authorize]
    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient([FromBody] CreateApiClientRequest request, CancellationToken cancellationToken = default)
    {
        var tenantClaim = User.FindFirst("tenant")?.Value;
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            return Unauthorized();
        }

        var (success, clientId, error) = await _authService.CreateApiClient(
            tenantId,
            request.Name,
            request.ClientId,
            request.ClientSecret,
            cancellationToken);

        if (success)
        {
            return Ok(new { ClientId = clientId });
        }
        return BadRequest(ApiErrorResponse.From(error ?? "Bad request"));
    }
}
