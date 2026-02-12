using Honeydew.Controllers.Models;
using Honeydew.Data;
using Honeydew.Entities;

namespace Honeydew.Services;

public class SupportTicketsService(SupportTicketsDataAccess data, UsersDataAccess usersData)
{
    private readonly SupportTicketsDataAccess _data = data;
    private readonly UsersDataAccess _usersData = usersData;

    public async Task<(List<SupportTicketResponse>? List, string? Error)> ListAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }

        var tickets = await _data.GetByTenantIdAsync(tenantId, cancellationToken);
        return (tickets.Select(Map).ToList(), null);
    }

    public async Task<(SupportTicketResponse? Ticket, string? Error)> CreateAsync(
        Guid userId,
        Guid tenantId,
        CreateSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return (null, "Subject is required.");
        }

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Subject = request.Subject.Trim(),
            Body = (request.Body ?? string.Empty).Trim(),
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        await _data.AddAsync(ticket, cancellationToken);
        return (Map(ticket), null);
    }

    public async Task<(SupportTicketWithRepliesResponse? Ticket, string? Error)> GetByIdWithRepliesAsync(
        Guid userId,
        Guid tenantId,
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }

        var ticket = await _data.GetByIdWithRepliesAsync(ticketId, tenantId, cancellationToken);
        if (ticket == null)
        {
            return (null, "NotFound");
        }
        return (MapWithReplies(ticket), null);
    }

    public async Task<(SupportTicketReplyResponse? Reply, string? Error)> AddReplyAsync(
        Guid userId,
        Guid tenantId,
        Guid ticketId,
        AddSupportTicketReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (null, "Unauthorized");
        }

        var ticket = await _data.GetByIdAsync(ticketId, tenantId, cancellationToken);
        if (ticket == null)
        {
            return (null, "NotFound");
        }
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return (null, "Body is required.");
        }

        var reply = new SupportTicketReply
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticketId,
            Body = request.Body.Trim(),
            IsFromStaff = false,
            CreatedAt = DateTime.UtcNow
        };
        await _data.AddReplyAsync(reply, cancellationToken);
        return (MapReply(reply), null);
    }

    public async Task<(bool Ok, string? Error)> UpdateStatusAsync(
        Guid userId,
        Guid tenantId,
        Guid ticketId,
        UpdateSupportTicketStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _usersData.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.TenantId != tenantId)
        {
            return (false, "Unauthorized");
        }
        var status = (request.Status ?? "Open").Trim();
        if (status != "Open" && status != "Closed")
        {
            return (false, "Status must be Open or Closed.");
        }
        var updated = await _data.UpdateStatusAsync(ticketId, tenantId, status, cancellationToken);
        return updated ? (true, null) : (false, "NotFound");
    }

    private static SupportTicketResponse Map(SupportTicket t)
    {
        return new SupportTicketResponse
        {
            Id = t.Id,
            TenantId = t.TenantId,
            Subject = t.Subject,
            Body = t.Body,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }

    private static SupportTicketWithRepliesResponse MapWithReplies(SupportTicket t)
    {
        return new SupportTicketWithRepliesResponse
        {
            Id = t.Id,
            TenantId = t.TenantId,
            Subject = t.Subject,
            Body = t.Body,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            Replies = t.Replies.Select(MapReply).ToList()
        };
    }

    private static SupportTicketReplyResponse MapReply(SupportTicketReply r)
    {
        return new SupportTicketReplyResponse
        {
            Id = r.Id,
            SupportTicketId = r.SupportTicketId,
            Body = r.Body,
            IsFromStaff = r.IsFromStaff,
            CreatedAt = r.CreatedAt
        };
    }
}
