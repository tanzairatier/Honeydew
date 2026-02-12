using System.Security.Claims;
using System.Text;
using Honeydew.Controllers.Models;
using Honeydew.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Honeydew.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TodosController(TodoService todoService) : ControllerBase
{
    private readonly TodoService _todoService = todoService;

    /// <summary>List todos with pagination and optional filters.</summary>
    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 9,
        [FromQuery] bool onlyMine = true,
        [FromQuery] bool includeCompleted = false,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] Guid[]? assignedToUserIds = null,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 3, 99);

        var (list, totalCount, error) = await _todoService.GetPageAsync(
            userId.Value, tenantId.Value, onlyMine, includeCompleted,
            assignedToUserIds?.Length > 0 ? assignedToUserIds : null,
            search, sortBy, sortDesc, page, pageSize, cancellationToken);
        if (error == "Forbid")
        {
            return Forbid();
        }
        if (list == null)
        {
            return Unauthorized();
        }
        return Ok(new { Items = list, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    /// <summary>Top N todos assigned to the current user (for home page).</summary>
    [HttpGet("assigned-to-me")]
    public async Task<ActionResult<List<TodoItemResponse>>> AssignedToMe(
        [FromQuery] int take = 3,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }
        take = Math.Clamp(take, 1, 20);
        var (list, error) = await _todoService.GetTopAssignedToMeAsync(userId.Value, tenantId.Value, take, cancellationToken);
        if (error != null)
        {
            return Unauthorized();
        }
        return Ok(list ?? new List<TodoItemResponse>());
    }

    /// <summary>Get a single todo by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoItemResponse>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (item, error) = await _todoService.GetByIdAsync(userId.Value, tenantId.Value, id, cancellationToken);
        if (error == "NotFound")
        {
            return NotFound(ApiErrorResponse.NotFound());
        }
        if (error == "Forbid")
        {
            return Forbid();
        }
        if (item == null)
        {
            return Unauthorized();
        }
        return Ok(item);
    }

    /// <summary>Create a todo.</summary>
    [HttpPost]
    public async Task<ActionResult<TodoItemResponse>> Create(
        [FromBody] CreateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (item, error) = await _todoService.CreateAsync(userId.Value, tenantId.Value, request, cancellationToken);
        if (error == "Forbid")
        {
            return Forbid();
        }
        if (error != null)
        {
            return BadRequest(ApiErrorResponse.From(error ?? "Bad request"));
        }
        return Ok(item);
    }

    /// <summary>Update a todo (e.g. mark done).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TodoItemResponse>> Update(
        Guid id,
        [FromBody] UpdateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (item, error) = await _todoService.UpdateAsync(userId.Value, tenantId.Value, id, request, cancellationToken);
        if (error == "NotFound")
        {
            return NotFound(ApiErrorResponse.NotFound());
        }
        if (error == "Forbid")
        {
            return Forbid();
        }
        if (item == null)
        {
            return Unauthorized();
        }
        return Ok(item);
    }

    /// <summary>Toggle thumbs-up vote on a todo. Returns current voted state (true = added, false = removed).</summary>
    [HttpPost("{id:guid}/vote")]
    public async Task<ActionResult<object>> ToggleVote(Guid id, CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }
        var (voted, error) = await _todoService.ToggleVoteAsync(userId.Value, tenantId.Value, id, cancellationToken);
        if (error != null)
        {
            return Unauthorized();
        }
        return Ok(new { Voted = voted });
    }

    /// <summary>Export todos as CSV. Query: onlyMine (default true).</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] bool onlyMine = true,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = GetUserAndTenantIdFromClaims();
        if (userId == null || tenantId == null)
        {
            return Unauthorized();
        }

        var (list, error) = await _todoService.GetAllForExportAsync(userId.Value, tenantId.Value, onlyMine, cancellationToken);
        if (error == "Forbid")
        {
            return Forbid();
        }
        if (list == null)
        {
            return Unauthorized();
        }

        var csv = BuildCsv(list);
        var bytes = Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "todos.csv");
    }

    private static string BuildCsv(List<TodoItemResponse> list)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Notes,IsDone,CompletedAt,DueDate,AssignedToUserId,CreatedAt");
        foreach (var t in list)
        {
            var notes = t.Notes == null ? "" : "\"" + t.Notes.Replace("\"", "\"\"") + "\"";
            var completed = t.CompletedAt?.ToString("o") ?? "";
            var due = t.DueDate?.ToString("o") ?? "";
            var assigned = t.AssignedToUserId?.ToString() ?? "";
            sb.AppendLine($"{t.Id},\"{t.Title.Replace("\"", "\"\"")}\",{notes},{t.IsDone},{completed},{due},{assigned},{t.CreatedAt:o}");
        }
        return sb.ToString();
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
