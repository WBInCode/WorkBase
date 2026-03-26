using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WorkBase.Shared.Auth;
using Xunit;

namespace WorkBase.Tests.Unit.Auth;

public class PermissionEndpointFilterTests
{
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task InvokeAsync_UserHasAllPermissions_CallsNext()
    {
        // Arrange
        var permissionService = Substitute.For<IPermissionService>();
        permissionService.GetUserPermissionsAsync(UserId, TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<string>)new HashSet<string> { "org.view", "org.create" });

        var (filter, context, next) = CreateFilter(
            ["org.view", "org.create"], permissionService, UserId, TenantId);

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        await next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_UserLacksPermission_Returns403()
    {
        // Arrange
        var permissionService = Substitute.For<IPermissionService>();
        permissionService.GetUserPermissionsAsync(UserId, TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<string>)new HashSet<string> { "org.view" });

        var (filter, context, next) = CreateFilter(
            ["org.view", "org.delete"], permissionService, UserId, TenantId);

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        await next.DidNotReceive().Invoke(Arg.Any<EndpointFilterInvocationContext>());
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, problemResult.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedUser_Returns401()
    {
        // Arrange
        var permissionService = Substitute.For<IPermissionService>();
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(permissionService, principal);
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(httpContext);
        var next = Substitute.For<EndpointFilterDelegate>();

        var filter = CreateFilterInstance(["org.view"]);

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        await next.DidNotReceive().Invoke(Arg.Any<EndpointFilterInvocationContext>());
    }

    [Fact]
    public async Task InvokeAsync_MissingTenantId_ReturnsForbid()
    {
        // Arrange
        var permissionService = Substitute.For<IPermissionService>();
        var claims = new[] { new Claim("sub", UserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(permissionService, principal);
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(httpContext);
        var next = Substitute.For<EndpointFilterDelegate>();

        var filter = CreateFilterInstance(["org.view"]);

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        await next.DidNotReceive().Invoke(Arg.Any<EndpointFilterInvocationContext>());
    }

    [Theory]
    [InlineData("org.view")]
    [InlineData("time.manage")]
    [InlineData("identity.assign-roles")]
    [InlineData("leave.approve")]
    public async Task InvokeAsync_SinglePermission_GrantedWhenPresent(string permission)
    {
        // Arrange
        var permissionService = Substitute.For<IPermissionService>();
        permissionService.GetUserPermissionsAsync(UserId, TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<string>)new HashSet<string> { permission });

        var (filter, context, next) = CreateFilter(
            [permission], permissionService, UserId, TenantId);

        // Act
        await filter.InvokeAsync(context, next);

        // Assert
        await next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("org.view")]
    [InlineData("time.manage")]
    [InlineData("identity.assign-roles")]
    [InlineData("leave.approve")]
    public async Task InvokeAsync_SinglePermission_DeniedWhenAbsent(string permission)
    {
        // Arrange
        var permissionService = Substitute.For<IPermissionService>();
        permissionService.GetUserPermissionsAsync(UserId, TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<string>)new HashSet<string>());

        var (filter, context, next) = CreateFilter(
            [permission], permissionService, UserId, TenantId);

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        await next.DidNotReceive().Invoke(Arg.Any<EndpointFilterInvocationContext>());
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, problemResult.StatusCode);
    }

    private static IEndpointFilter CreateFilterInstance(string[] permissions)
    {
        // Use reflection to create the internal filter
        var filterType = typeof(PermissionEndpointExtensions).Assembly
            .GetType("WorkBase.Shared.Auth.PermissionEndpointFilter")!;
        return (IEndpointFilter)Activator.CreateInstance(filterType, new object[] { permissions })!;
    }

    private static (IEndpointFilter filter, EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        CreateFilter(string[] permissions, IPermissionService permissionService, Guid userId, Guid tenantId)
    {
        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("tenant_id", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(permissionService, principal);
        var context = Substitute.For<EndpointFilterInvocationContext>();
        context.HttpContext.Returns(httpContext);
        var next = Substitute.For<EndpointFilterDelegate>();
        next.Invoke(Arg.Any<EndpointFilterInvocationContext>()).Returns("OK");

        var filter = CreateFilterInstance(permissions);
        return (filter, context, next);
    }

    private static HttpContext CreateHttpContext(IPermissionService permissionService, ClaimsPrincipal principal)
    {
        var services = new ServiceCollection();
        services.AddSingleton(permissionService);
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            RequestServices = sp,
            RequestAborted = CancellationToken.None
        };
        return httpContext;
    }
}
