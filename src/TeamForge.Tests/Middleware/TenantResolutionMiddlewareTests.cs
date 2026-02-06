using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TeamForge.Api.Middleware;
using TeamForge.Data;
using Xunit;

namespace TeamForge.Tests.Middleware;

public class TenantResolutionMiddlewareTests : IDisposable
{
    private readonly TeamForgeDbContext _db;
    private readonly Mock<ILogger<TenantResolutionMiddleware>> _loggerMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public TenantResolutionMiddlewareTests()
    {
        var options = new DbContextOptionsBuilder<TeamForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TeamForgeDbContext(options);
        _loggerMock = new Mock<ILogger<TenantResolutionMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_SetsTenantIdInItems()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("tenant_id", _tenantId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "Bearer"));

        // Act
        await middleware.InvokeAsync(context, _db);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(_tenantId, context.Items["TenantId"]);
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedUser_DoesNotSetTenantId()
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, _loggerMock.Object);

        var context = new DefaultHttpContext();
        // No user / no auth

        await middleware.InvokeAsync(context, _db);

        Assert.True(nextCalled);
        Assert.False(context.Items.ContainsKey("TenantId"));
    }

    [Fact]
    public async Task InvokeAsync_MissingTenantClaim_DoesNotSetTenantId()
    {
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask, _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "Bearer"));

        await middleware.InvokeAsync(context, _db);

        Assert.False(context.Items.ContainsKey("TenantId"));
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
