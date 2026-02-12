using System.Security.Claims;
using Honeydew.Controllers.Models;
using Honeydew.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Honeydew.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantController(TenantService tenantService) : ControllerBase
{
    private readonly TenantService _tenantService = tenantService;

    /// <summary>Get current tenant details (from JWT).</summary>
    [HttpGet]
    public async Task<ActionResult<TenantResponse>> Get(CancellationToken cancellationToken)
    {
        var tenantId = GetTenantIdFromClaims();
        if (tenantId == null)
        {
            return Unauthorized();
        }

        var tenant = await _tenantService.GetTenantAsync(tenantId.Value, cancellationToken);
        if (tenant == null)
        {
            return NotFound(ApiErrorResponse.NotFound("Tenant not found."));
        }

        return Ok(tenant);
    }

    /// <summary>Update tenant (e.g. household name).</summary>
    [HttpPatch]
    public async Task<ActionResult<TenantResponse>> Update([FromBody] UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdFromClaims();
        if (tenantId == null)
        {
            return Unauthorized();
        }
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiErrorResponse.Validation("Name is required."));
        }

        var updated = await _tenantService.UpdateNameAsync(tenantId.Value, request.Name, cancellationToken);
        if (!updated)
        {
            return NotFound(ApiErrorResponse.NotFound("Tenant not found."));
        }
        var tenant = await _tenantService.GetTenantAsync(tenantId.Value, cancellationToken);
        return Ok(tenant);
    }

    /// <summary>Set tenant billing plan.</summary>
    [HttpPut("billing-plan")]
    public async Task<ActionResult<TenantResponse>> SetBillingPlan([FromBody] SetBillingPlanRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantIdFromClaims();
        if (tenantId == null)
        {
            return Unauthorized();
        }

        var updated = await _tenantService.UpdateBillingPlanAsync(tenantId.Value, request.BillingPlanId, cancellationToken);
        if (!updated)
        {
            return NotFound(ApiErrorResponse.NotFound("Tenant not found."));
        }
        var tenant = await _tenantService.GetTenantAsync(tenantId.Value, cancellationToken);
        return Ok(tenant);
    }

    private Guid? GetTenantIdFromClaims()
    {
        var claim = User.FindFirst("tenant")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
