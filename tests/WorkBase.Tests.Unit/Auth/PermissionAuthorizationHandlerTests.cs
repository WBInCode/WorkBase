using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WorkBase.Infrastructure.Auth;
using WorkBase.Shared.Auth;
using Xunit;

namespace WorkBase.Tests.Unit.Auth;

public class PermissionAuthorizationHandlerTests
{
    private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();
    private readonly ILogger<PermissionAuthorizationHandler> _logger = Substitute.For<ILogger<PermissionAuthorizationHandler>>();
    private readonly PermissionAuthorizationHandler _handler;

    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public PermissionAuthorizationHandlerTests()
    {
        _handler = new PermissionAuthorizationHandler(_permissionService, _logger);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserHasPermission_Succeeds()
    {
        // Arrange
        var requirement = new PermissionRequirement("org.view");
        var context = CreateAuthorizationContext(requirement, UserId, TenantId);

        _permissionService.HasPermissionAsync(UserId, TenantId, "org.view", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserLacksPermission_DoesNotSucceed()
    {
        // Arrange
        var requirement = new PermissionRequirement("org.delete");
        var context = CreateAuthorizationContext(requirement, UserId, TenantId);

        _permissionService.HasPermissionAsync(UserId, TenantId, "org.delete", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_MissingUserId_DoesNotSucceed()
    {
        // Arrange
        var requirement = new PermissionRequirement("org.view");
        var claims = new[] { new Claim("tenant_id", TenantId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, principal, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_MissingTenantId_DoesNotSucceed()
    {
        // Arrange
        var requirement = new PermissionRequirement("org.view");
        var claims = new[] { new Claim("sub", UserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, principal, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Theory]
    [InlineData("org.view")]
    [InlineData("org.create")]
    [InlineData("org.edit")]
    [InlineData("org.delete")]
    [InlineData("org.export")]
    [InlineData("org.import")]
    [InlineData("time.view")]
    [InlineData("time.manage")]
    [InlineData("time.approve")]
    [InlineData("time.view-team")]
    [InlineData("leave.view")]
    [InlineData("leave.approve")]
    [InlineData("leave.view-team")]
    [InlineData("identity.manage")]
    [InlineData("identity.assign-roles")]
    [InlineData("identity.manage-feature-flags")]
    [InlineData("tasks.view")]
    [InlineData("tasks.assign")]
    [InlineData("workflow.approve")]
    [InlineData("dashboard.view")]
    [InlineData("notification.view")]
    [InlineData("documents.view")]
    [InlineData("documents.import")]
    public async Task HandleRequirementAsync_EachPermissionCode_CheckedCorrectly(string permission)
    {
        // Arrange
        var requirement = new PermissionRequirement(permission);
        var context = CreateAuthorizationContext(requirement, UserId, TenantId);

        _permissionService.HasPermissionAsync(UserId, TenantId, permission, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
        await _permissionService.Received(1)
            .HasPermissionAsync(UserId, TenantId, permission, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Super Admin")]
    [InlineData("Admin")]
    [InlineData("Kierownik")]
    [InlineData("Pracownik")]
    [InlineData("HR")]
    public async Task HandleRequirementAsync_RoleBasedPermission_DelegatedToService(string roleName)
    {
        // The handler doesn't check roles directly — it delegates to IPermissionService
        // This test verifies the service is called regardless of role name in claims
        var requirement = new PermissionRequirement("org.view");
        var claims = new[]
        {
            new Claim("sub", UserId.ToString()),
            new Claim("tenant_id", TenantId.ToString()),
            new Claim("roles", roleName)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, principal, null);

        _permissionService.HasPermissionAsync(UserId, TenantId, "org.view", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext(
        PermissionRequirement requirement,
        Guid userId,
        Guid tenantId)
    {
        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("tenant_id", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return new AuthorizationHandlerContext(
            new[] { requirement }, principal, null);
    }
}
