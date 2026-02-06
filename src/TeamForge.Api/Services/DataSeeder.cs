using TeamForge.Data;
using TeamForge.Data.Entities;

namespace TeamForge.Api.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(TeamForgeDbContext db)
    {
        if (db.Tenants.Any()) return;

        // ── Tenant 1: Acme Corp (blue theme) ──
        var acmeId = Guid.NewGuid();
        var acmeAdmin = CreateTenant(db, acmeId, "Acme Corp", "acme",
            "#1565c0", "#ff8f00", "#43a047", "#fafafa", "#212121",
            "Roboto", "Building the future, one project at a time",
            "admin@acme.com", "Alice Johnson");

        AddTeams(db, acmeId, acmeAdmin,
            ("Engineering", "Core product development"),
            ("Marketing", "Brand strategy and campaigns"),
            ("Sales", "Revenue growth and partnerships"));

        AddProjects(db, acmeId,
            ("Project Atlas", "Enterprise platform redesign", "Development"),
            ("Q1 Campaign", "Spring marketing push", "Marketing"),
            ("Client Portal", "Self-service customer dashboard", "Development"));

        AddAnnouncement(db, acmeId, acmeAdmin,
            "Welcome to Acme Corp!", "We're excited to launch our new TeamForge portal. Check out your teams and projects!");

        // ── Tenant 2: Pixel Studio (purple theme) ──
        var pixelId = Guid.NewGuid();
        var pixelAdmin = CreateTenant(db, pixelId, "Pixel Studio", "pixel",
            "#7b1fa2", "#f57c00", "#00897b", "#f5f5f5", "#1a1a1a",
            "Inter", "Where creativity meets precision",
            "admin@pixelstudio.com", "Max Rivera");

        AddTeams(db, pixelId, pixelAdmin,
            ("UX Design", "User experience research and design"),
            ("Visual Design", "Brand and visual identity"),
            ("Frontend", "Web and mobile development"));

        AddProjects(db, pixelId,
            ("Brand Refresh", "Complete visual identity overhaul", "Design"),
            ("Mobile App", "iOS and Android native app", "Development"),
            ("Design System", "Component library and style guide", "Design"));

        AddAnnouncement(db, pixelId, pixelAdmin,
            "Welcome Pixels!", "Our creative hub is live. Explore your teams and start collaborating!");

        // ── Tenant 3: GreenLeaf (green theme) ──
        var greenId = Guid.NewGuid();
        var greenAdmin = CreateTenant(db, greenId, "GreenLeaf Solutions", "greenleaf",
            "#2e7d32", "#ff6f00", "#0277bd", "#f1f8e9", "#1b5e20",
            "Nunito", "Sustainable solutions for a better tomorrow",
            "admin@greenleaf.com", "Sam Chen");

        AddTeams(db, greenId, greenAdmin,
            ("Research", "Environmental impact research"),
            ("Operations", "Day-to-day business operations"),
            ("Outreach", "Community engagement and partnerships"));

        AddProjects(db, greenId,
            ("Carbon Tracker", "Carbon footprint monitoring tool", "Research"),
            ("Community Garden", "Urban farming initiative", "Outreach"),
            ("Solar Initiative", "Renewable energy adoption program", "Operations"));

        AddAnnouncement(db, greenId, greenAdmin,
            "Welcome to GreenLeaf!", "Together we're building a more sustainable future. Dive into your projects!");

        await db.SaveChangesAsync();
    }

    private static Guid CreateTenant(TeamForgeDbContext db, Guid tenantId, string name, string subdomain,
        string primary, string secondary, string accent, string bg, string text,
        string font, string tagline, string adminEmail, string adminName)
    {
        var tenant = new Tenant
        {
            Id = tenantId,
            CompanyName = name,
            Subdomain = subdomain,
            Description = tagline
        };
        db.Tenants.Add(tenant);

        db.TenantBranding.Add(new TenantBranding
        {
            TenantId = tenantId,
            PrimaryColor = primary,
            SecondaryColor = secondary,
            AccentColor = accent,
            BackgroundColor = bg,
            TextColor = text,
            FontFamily = font,
            TagLine = tagline
        });

        // Create roles
        var adminRole = new Role { TenantId = tenantId, Name = "Admin", Description = "Full access" };
        var leadRole = new Role { TenantId = tenantId, Name = "Lead", Description = "Team management" };
        var memberRole = new Role { TenantId = tenantId, Name = "Member", Description = "Standard access" };
        db.Roles.AddRange(adminRole, leadRole, memberRole);

        // Create admin user
        var adminUser = new AppUser
        {
            TenantId = tenantId,
            Email = adminEmail,
            DisplayName = adminName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("demo123!"),
            LastLoginAt = DateTime.UtcNow
        };
        db.AppUsers.Add(adminUser);

        db.UserRoles.Add(new UserRole
        {
            TenantId = tenantId,
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        return adminUser.Id;
    }

    private static void AddTeams(TeamForgeDbContext db, Guid tenantId, Guid adminUserId,
        params (string Name, string Desc)[] teams)
    {
        foreach (var (name, desc) in teams)
        {
            var team = new Team
            {
                TenantId = tenantId,
                Name = name,
                Description = desc
            };
            db.Teams.Add(team);

            db.TeamMembers.Add(new TeamMember
            {
                TenantId = tenantId,
                TeamId = team.Id,
                UserId = adminUserId,
                Role = "Lead"
            });
        }
    }

    private static void AddProjects(TeamForgeDbContext db, Guid tenantId,
        params (string Name, string Desc, string Category)[] projects)
    {
        foreach (var (name, desc, category) in projects)
        {
            db.Projects.Add(new Project
            {
                TenantId = tenantId,
                Name = name,
                Description = desc,
                Category = category
            });
        }
    }

    private static void AddAnnouncement(TeamForgeDbContext db, Guid tenantId, Guid userId,
        string title, string content)
    {
        db.Announcements.Add(new Announcement
        {
            TenantId = tenantId,
            Title = title,
            Content = content,
            CreatedByUserId = userId
        });
    }
}
