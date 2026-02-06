using Microsoft.EntityFrameworkCore;
using TeamForge.Data.Entities;

namespace TeamForge.Data;

public class TeamForgeDbContext : DbContext
{
    public TeamForgeDbContext(DbContextOptions<TeamForgeDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantBranding> TenantBranding => Set<TenantBranding>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("tenant");

        // Tenant — no RLS (lookup table)
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.Subdomain)
                .IsUnique()
                .HasFilter("[Subdomain] IS NOT NULL")
                .HasDatabaseName("IX_Tenants_Subdomain");

            entity.HasOne(e => e.Branding)
                .WithOne(b => b.Tenant)
                .HasForeignKey<TenantBranding>(b => b.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TenantBranding
        modelBuilder.Entity<TenantBranding>(entity =>
        {
            entity.HasIndex(e => e.TenantId)
                .IsUnique()
                .HasDatabaseName("IX_TenantBranding_TenantId");
        });

        // AppUser
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Email })
                .IsUnique()
                .HasDatabaseName("IX_AppUsers_TenantId_Email");

            entity.HasIndex(e => e.EntraIdObjectId)
                .IsUnique()
                .HasFilter("[EntraIdObjectId] IS NOT NULL")
                .HasDatabaseName("IX_AppUsers_EntraIdObjectId");
        });

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name })
                .IsUnique()
                .HasDatabaseName("IX_Roles_TenantId_Name");
        });

        // UserRole
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId })
                .IsUnique()
                .HasDatabaseName("IX_UserRoles_UserId_RoleId");

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Project
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name })
                .HasDatabaseName("IX_Projects_TenantId_Name");
        });

        // Team
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name })
                .IsUnique()
                .HasDatabaseName("IX_Teams_TenantId_Name");
        });

        // TeamMember
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasIndex(e => new { e.TeamId, e.UserId })
                .IsUnique()
                .HasDatabaseName("IX_TeamMembers_TeamId_UserId");

            entity.HasOne(e => e.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Announcement
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Announcements_TenantId");

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    /// <summary>
    /// Sets SESSION_CONTEXT for Row-Level Security. Called by TenantResolutionMiddleware.
    /// No-op for InMemory databases (dev/test).
    /// </summary>
    public async Task SetTenantContextAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory") return;

        await Database.ExecuteSqlRawAsync(
            "EXEC sp_set_session_context @key = N'TenantId', @value = {0}",
            new object[] { tenantId },
            cancellationToken);
    }
}
