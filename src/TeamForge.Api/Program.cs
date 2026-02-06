using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeamForge.Api.Middleware;
using TeamForge.Api.Services;
using TeamForge.Data;

var builder = WebApplication.CreateBuilder(args);

// ── JSON serialization ──
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ── Swagger ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "TeamForge API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ── Database ──
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<TeamForgeDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    // In-memory for development/demo/CI
    builder.Services.AddDbContext<TeamForgeDbContext>(options =>
        options.UseInMemoryDatabase("TeamForge"));
}

// ── JWT Authentication ──
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsDevelopment())
        jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    else
        throw new InvalidOperationException("JWT Key must be configured in production");
}

// Store generated key so AuthService can read it
builder.Configuration["Jwt:Key"] = jwtKey;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TeamForge",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TeamForgeClient",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ── Services ──
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();

// AI Agent: real Claude if API key configured, mock otherwise
var claudeApiKey = builder.Configuration["Claude:ApiKey"];
if (!string.IsNullOrEmpty(claudeApiKey))
{
    builder.Services.AddHttpClient<IAiAgentService, ClaudeAgentService>();
}
else
{
    builder.Services.AddSingleton<IAiAgentService, MockAiAgentService>();
}

// ── Health checks ──
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Seed demo data ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TeamForgeDbContext>();
    if (db.Database.IsInMemory())
    {
        db.Database.EnsureCreated();
    }
    await DataSeeder.SeedAsync(db);
}

// ── Middleware pipeline (order matters!) ──
app.UseGlobalExceptionHandling();
app.UseRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TeamForge API v1"));
}

app.UseCors(policy => policy
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantResolution(); // After auth (needs JWT), before controllers (sets SESSION_CONTEXT)

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
