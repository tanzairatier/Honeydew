using System.Security.Claims;
using Honeydew.Controllers.Models;
using Honeydew.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Honeydew.Controllers;

[ApiController]
[Route("api/support-tickets")]
[Authorize]
public class SupportTicketsController(SupportTicketsService service) : ControllerBase
{
    private readonly SupportTicketsService _service = service;

    [HttpGet]
    public async Task<ActionResult<List<SupportTicketResponse>>> List(CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (list, error) = await _service.ListAsync(userId.Value, tenantId.Value, cancellationToken);
        if (error != null)
        {
            return Unauthorized();
        }
        return Ok(list ?? []);
    }

    [HttpPost]
    public async Task<ActionResult<SupportTicketResponse>> Create(
        [FromBody] CreateSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (ticket, error) = await _service.CreateAsync(userId.Value, tenantId.Value, request, cancellationToken);
        if (error == "Unauthorized")
        {
            return Unauthorized();
        }
        if (error != null)
        {
            return BadRequest(ApiErrorResponse.From(error ?? "Bad request"));
        }
        return Ok(ticket);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupportTicketWithRepliesResponse>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (ticket, error) = await _service.GetByIdWithRepliesAsync(userId.Value, tenantId.Value, id, cancellationToken);
        if (error == "NotFound")
        {
            return NotFound(ApiErrorResponse.NotFound());
        }
        if (error != null)
        {
            return Unauthorized();
        }
        return Ok(ticket);
    }

    [HttpPost("{id:guid}/replies")]
    public async Task<ActionResult<SupportTicketReplyResponse>> AddReply(
        Guid id,
        [FromBody] AddSupportTicketReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (reply, error) = await _service.AddReplyAsync(userId.Value, tenantId.Value, id, request, cancellationToken);
        if (error == "NotFound")
        {
            return NotFound(ApiErrorResponse.NotFound());
        }
        if (error != null)
        {
            return BadRequest(ApiErrorResponse.From(error ?? "Bad request"));
        }
        return Ok(reply);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateSupportTicketStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (_, error) = await _service.UpdateStatusAsync(userId.Value, tenantId.Value, id, request, cancellationToken);
        if (error == "NotFound")
        {
            return NotFound(ApiErrorResponse.NotFound());
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
