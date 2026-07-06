namespace WorkBase.Shared.Modules;

/// <summary>
/// Resolves which business module (from <see cref="ModuleCatalog"/>) a CQRS request type
/// belongs to, based on its namespace convention:
/// <c>WorkBase.Modules.{Namespace}.Application.(Commands|Queries).{RequestName}</c>.
///
/// Used by module-gating enforcement (see docs/05-module-licensing-architecture.md §4) so
/// that no handler needs to be manually annotated — it works automatically for every
/// existing and future module as long as the standard namespace convention is followed.
/// </summary>
public static class ModuleResolver
{
    private const string ModulesNamespacePrefix = "WorkBase.Modules.";

    /// <summary>
    /// Returns the module's short <see cref="ModuleInfo.Key"/> for the given request type,
    /// or null if the type does not belong to any cataloged module (e.g. platform-level
    /// requests defined outside src/Modules) — such requests are not subject to
    /// module-based licensing checks.
    /// </summary>
    public static string? ResolveModuleKey(Type requestType)
    {
        var ns = requestType.Namespace;
        if (ns is null || !ns.StartsWith(ModulesNamespacePrefix, StringComparison.Ordinal))
            return null;

        var remainder = ns[ModulesNamespacePrefix.Length..];
        var moduleNamespace = remainder.Split('.')[0];

        return ModuleCatalog.FindByNamespace(moduleNamespace)?.Key;
    }
}
