using System.Security.Claims;
using Honeydew.Controllers.Models;
using Honeydew.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Honeydew.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(UsersService usersService, UserPreferencesService preferencesService) : ControllerBase
{
    private readonly UsersService _usersService = usersService;
    private readonly UserPreferencesService _preferencesService = preferencesService;

    /// <summary>List users in the current tenant. Owner or CanCreateUser; or any member if forAssignmentOnly=true.</summary>
    [HttpGet]
    public async Task<ActionResult<List<UserSummaryResponse>>> List(
        [FromQuery] bool activeOnly = false,
        [FromQuery] bool forAssignmentOnly = false,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (list, error) = await _usersService.ListUsersAsync(userId.Value, tenantId.Value, activeOnly, forAssignmentOnly, cancellationToken);
        if (error == "Forbid")
        {
            return Forbid();
        }
        if (list == null)
        {
            return Unauthorized();
        }
        return Ok(list);
    }

    /// <summary>Get current user profile (for role, permissions, display name, email).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserSummaryResponse>> GetMe(CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (user, error) = await _usersService.GetCurrentUserAsync(userId.Value, tenantId.Value, cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }
        return Ok(user);
    }

    /// <summary>Update current user's display name and/or email.</summary>
    [HttpPatch("me")]
    public async Task<ActionResult<UserSummaryResponse>> UpdateMe(
        [FromBody] UpdateCurrentUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (user, error) = await _usersService.UpdateCurrentUserAsync(userId.Value, tenantId.Value, request, cancellationToken);
        if (error != null)
        {
            return BadRequest(ApiErrorResponse.From(error ?? "Bad request"));
        }
        return Ok(user);
    }

    [HttpGet("me/preferences")]
    public async Task<ActionResult<UserPreferencesResponse>> GetMyPreferences(CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }
        var prefs = await _preferencesService.GetAsync(userId.Value, cancellationToken);
        if (prefs == null)
        {
            return Unauthorized();
        }
        return Ok(prefs);
    }

    [HttpPut("me/preferences")]
    public async Task<ActionResult<UserPreferencesResponse>> UpdateMyPreferences(
        [FromBody] UserPreferencesResponse request,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }
        var (prefs, error) = await _preferencesService.SetItemsPerPageAsync(userId.Value, request.ItemsPerPage, cancellationToken);
        if (error != null)
        {
            return BadRequest(ApiErrorResponse.From(error ?? "Bad request"));
        }
        return Ok(prefs);
    }

    /// <summary>Create a user in the tenant. Owner or CanCreateUser.</summary>
    [HttpPost]
    public async Task<ActionResult<UserSummaryResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (user, error) = await _usersService.CreateUserAsync(userId.Value, tenantId.Value, request, cancellationToken);
        if (error == "Forbid")
        {
            return Forbid();
        }
        if (error != null)
        {
            return BadRequest(ApiErrorResponse.From(error ?? "Bad request"));
        }
        return Ok(user);
    }

    /// <summary>Update a user. Owner can update any user's role and flags.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserSummaryResponse>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (user, error) = await _usersService.UpdateUserAsync(userId.Value, tenantId.Value, id, request, cancellationToken);
        if (error == "NotFound")
        {
            return NotFound(ApiErrorResponse.NotFound());
        }
        if (error != null)
        {
            return Unauthorized();
        }
        return Ok(user);
    }

    /// <summary>Delete a user. Owner can delete any non-owner; any user can delete themselves. Cannot delete last owner.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (ok, error) = await _usersService.DeleteUserAsync(userId.Value, tenantId.Value, id, cancellationToken);
        if (error == "NotFound")
        {
            return NotFound(ApiErrorResponse.NotFound());
        }
        if (error == "Forbid")
        {
            return Forbid();
        }
        if (error != null)
        {
            return BadRequest(ApiErrorResponse.From(error ?? "Bad request"));
        }
        return NoContent();
    }

    private (Guid? UserId, Guid? TenantId) GetUserAndTenantIdFromClaims()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var tenant = User.FindFirst("tenant")?.Value;
        var userId = Guid.TryParse(sub, out var u) ? u : (Guid?)null;
        var tenantId = Guid.TryParse(tenant, out var t) ? t : (Guid?)null;
        return (userId, tenantId);
    }
}
