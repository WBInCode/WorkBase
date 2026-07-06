namespace WorkBase.Shared.Modules;

/// <summary>
/// Broad licensing/pricing grouping for a module. Used to build default bundles
/// (e.g. Bronze/Silver/Gold) — see docs/05-module-licensing-architecture.md.
/// </summary>
public enum ModuleGroup
{
    /// <summary>Always included — required baseline for the platform to function.</summary>
    Core,

    /// <summary>Mid-tier add-ons commonly bundled with Core.</summary>
    Standard,

    /// <summary>Premium/enterprise add-ons.</summary>
    Premium,
}

/// <summary>
/// Describes a single business module of the platform.
/// </summary>
/// <param name="Key">
/// Short, stable identifier used in permissions ("{Key}.view"), feature flags
/// (<c>iam_feature_flags.Module</c>) and data scopes. Must never change once shipped.
/// </param>
/// <param name="Namespace">
/// The module's folder/namespace segment under <c>src/Modules/{Namespace}</c>,
/// e.g. "TimeTracking". Used to resolve assembly names for module discovery and
/// architecture boundary tests.
/// </param>
/// <param name="DisplayName">Human-readable Polish name shown in admin UI.</param>
/// <param name="Group">Licensing tier grouping (see <see cref="ModuleGroup"/>).</param>
public sealed record ModuleInfo(string Key, string Namespace, string DisplayName, ModuleGroup Group);

/// <summary>
/// Single source of truth for the list of business modules in the platform.
/// Consumed by:
/// - <c>ModuleDiscovery</c> (backend module/endpoint registration)
/// - <c>IamSeeder</c> (permissions, data scopes, feature flags)
/// - <c>ModuleBoundaryTests</c> (architecture isolation tests)
/// - Frontend admin UI (module labels, navigation) via the modules endpoint
///
/// Adding a new module: append one entry here. Do not remove/rename existing
/// Key/Namespace values — they are persisted in the database (permissions,
/// feature flags) and referenced by Keycloak/tenant configuration.
/// </summary>
public static class ModuleCatalog
{
    public static readonly ModuleInfo[] All =
    [
        new("org", "Organization", "Organizacja", ModuleGroup.Core),
        new("identity", "Identity", "Zarządzanie dostępem", ModuleGroup.Core),
        new("time", "TimeTracking", "Czas pracy", ModuleGroup.Core),
        new("leave", "Leave", "Urlopy", ModuleGroup.Core),
        new("tasks", "Tasks", "Zadania", ModuleGroup.Core),
        new("workflow", "Workflow", "Procesy", ModuleGroup.Core),
        new("dashboard", "Dashboard", "Dashboard", ModuleGroup.Core),
        new("notification", "Notification", "Powiadomienia", ModuleGroup.Core),
        new("documents", "Documents", "Dokumenty", ModuleGroup.Standard),
        new("integration", "Integration", "Integracje", ModuleGroup.Standard),
        new("forms", "Forms", "Formularze", ModuleGroup.Standard),
        new("cases", "Cases", "Sprawy", ModuleGroup.Premium),
        new("contacts", "Contacts", "Kontakty", ModuleGroup.Premium),
        new("sales", "Sales", "Sprzedaż", ModuleGroup.Premium),
        new("ai", "AI", "AI", ModuleGroup.Premium),
    ];

    /// <summary>Looks up a module by its short key. Returns null if not found.</summary>
    public static ModuleInfo? FindByKey(string key) =>
        All.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Looks up a module by its namespace/folder segment. Returns null if not found.</summary>
    public static ModuleInfo? FindByNamespace(string ns) =>
        All.FirstOrDefault(m => string.Equals(m.Namespace, ns, StringComparison.OrdinalIgnoreCase));
}
