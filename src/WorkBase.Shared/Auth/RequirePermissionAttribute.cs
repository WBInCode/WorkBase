namespace WorkBase.Shared.Auth;

/// <summary>
/// Specifies the permission required to access an endpoint.
/// Used with Minimal APIs via RequirePermission() extension method.
/// Format: "module.action" (e.g., "org.view", "time.manage", "identity.assign-roles")
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string permission) : Attribute
{
    public string Permission { get; } = permission;
}
