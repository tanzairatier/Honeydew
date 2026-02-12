using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeydew.Data;

public class SupportTicketsDataAccess(HoneydewDbContext db)
{
    private readonly HoneydewDbContext _db = db;

    public async Task<List<SupportTicket>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.SupportTickets
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, cancellationToken);
    }

    public async Task<SupportTicket?> GetByIdWithRepliesAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.SupportTickets
            .AsNoTracking()
            .Include(t => t.Replies.OrderBy(r => r.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, cancellationToken);
    }

    public async Task<SupportTicket> AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task<SupportTicketReply> AddReplyAsync(SupportTicketReply reply, CancellationToken cancellationToken = default)
    {
        _db.SupportTicketReplies.Add(reply);
        await _db.SaveChangesAsync(cancellationToken);
        return reply;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, Guid tenantId, string status, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, cancellationToken);
        if (ticket == null)
        {
            return false;
        }
        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
