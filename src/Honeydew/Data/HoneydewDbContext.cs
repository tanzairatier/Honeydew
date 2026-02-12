using Honeydew.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeydew.Data;

public class HoneydewDbContext(DbContextOptions<HoneydewDbContext> options) : DbContext(options)
{

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ApiClient> ApiClients => Set<ApiClient>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TodoItemVote> TodoItemVotes => Set<TodoItemVote>();
    public DbSet<BillingPlan> BillingPlans => Set<BillingPlan>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportTicketReply> SupportTicketReplies => Set<SupportTicketReply>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BillingPlan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Code).HasMaxLength(32);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256);
            e.HasIndex(x => x.Name);
            e.HasOne(x => x.BillingPlan).WithMany().HasForeignKey(x => x.BillingPlanId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SupportTicket>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Subject).HasMaxLength(500);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.Property(x => x.Status).HasMaxLength(32);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Tenant).WithMany(t => t.SupportTickets).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Replies).WithOne(x => x.SupportTicket).HasForeignKey(x => x.SupportTicketId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupportTicketReply>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.HasIndex(x => x.SupportTicketId);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(256);
            e.Property(x => x.PasswordHash).HasMaxLength(500);
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(t => t.Users).HasForeignKey(x => x.TenantId);
            e.Property(x => x.CanCreateUser);
        });

        modelBuilder.Entity<UserPreference>(e =>
        {
            e.HasKey(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiClient>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ClientId).HasMaxLength(128);
            e.Property(x => x.ClientSecretHash).HasMaxLength(500);
            e.Property(x => x.Name).HasMaxLength(256);
            e.HasIndex(x => x.ClientId).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(t => t.ApiClients).HasForeignKey(x => x.TenantId);
        });

        modelBuilder.Entity<TodoItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.CreatedByUserId);
            e.HasIndex(x => x.AssignedToUserId);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        modelBuilder.Entity<TodoItemVote>(e =>
        {
            e.HasKey(x => new { x.TodoItemId, x.UserId });
            e.HasIndex(x => x.TodoItemId);
            e.HasOne(x => x.TodoItem).WithMany().HasForeignKey(x => x.TodoItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
