using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Modules;

namespace WorkBase.Infrastructure.Seeding;

public static class IamSeeder
{
    // Deterministic GUIDs for seed data consistency across environments
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // System role IDs
    private static readonly Guid SuperAdminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid AdminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    // Template role IDs
    private static readonly Guid KierownikRoleId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid PracownikRoleId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid HrRoleId = Guid.Parse("10000000-0000-0000-0000-000000000005");

    // Module names — sourced from the single ModuleCatalog (src/WorkBase.Shared/Modules/ModuleCatalog.cs)
    // so newly added modules automatically get baseline CRUD permissions, data scopes and a
    // feature flag row (enabled by default for the seeded default tenant).
    private static class Modules
    {
        public const string Organization = "org";
        public const string Identity = "identity";
        public const string Time = "time";
        public const string Leave = "leave";
        public const string Tasks = "tasks";
        public const string Workflow = "workflow";
        public const string Dashboard = "dashboard";
        public const string Notification = "notification";
        public const string Documents = "documents";

        public static readonly string[] All = ModuleCatalog.All.Select(m => m.Key).ToArray();
    }

    // Standard CRUD actions
    private static class Actions
    {
        public const string View = "view";
        public const string Create = "create";
        public const string Edit = "edit";
        public const string Delete = "delete";
        public const string Manage = "manage";
        public const string Export = "export";
        public const string Import = "import";
    }

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<Permission>().AnyAsync())
        {
            logger.LogInformation("IAM data already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding IAM permissions...");
        var permissions = CreatePermissions();
        dbContext.Set<Permission>().AddRange(permissions);

        logger.LogInformation("Seeding IAM roles...");
        var roles = CreateRoles();
        dbContext.Set<Role>().AddRange(roles);

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeding role-permission assignments...");
        var rolePermissions = CreateRolePermissions(permissions);
        dbContext.Set<RolePermission>().AddRange(rolePermissions);

        logger.LogInformation("Seeding data scopes...");
        var dataScopes = CreateDataScopes();
        dbContext.Set<DataScope>().AddRange(dataScopes);

        logger.LogInformation("Seeding feature flags...");
        var featureFlags = CreateFeatureFlags();
        dbContext.Set<FeatureFlag>().AddRange(featureFlags);

        await dbContext.SaveChangesAsync();

        logger.LogInformation("IAM seeding completed: {PermCount} permissions, {RoleCount} roles, {RpCount} role-permissions, {DsCount} data scopes, {FfCount} feature flags.",
            permissions.Count, roles.Count, rolePermissions.Count, dataScopes.Count, featureFlags.Count);
    }

    private static List<Permission> CreatePermissions()
    {
        var permissions = new List<Permission>();
        var permissionId = 1;

        // Standard CRUD permissions for each module
        foreach (var module in Modules.All)
        {
            permissions.Add(CreatePermission(permissionId++, module, Actions.View, description: $"Przeglądanie danych modułu {module}"));
            permissions.Add(CreatePermission(permissionId++, module, Actions.Create, description: $"Tworzenie rekordów w module {module}"));
            permissions.Add(CreatePermission(permissionId++, module, Actions.Edit, description: $"Edycja rekordów w module {module}"));
            permissions.Add(CreatePermission(permissionId++, module, Actions.Delete, description: $"Usuwanie rekordów w module {module}"));
            permissions.Add(CreatePermission(permissionId++, module, Actions.Export, description: $"Eksport danych z modułu {module}"));
        }

        // Module-specific permissions
        // Organization
        permissions.Add(CreatePermission(permissionId++, Modules.Organization, Actions.Import, description: "Import pracowników (CSV)"));
        permissions.Add(CreatePermission(permissionId++, Modules.Organization, Actions.Manage, description: "Zarządzanie strukturą organizacyjną"));

        // Identity
        permissions.Add(CreatePermission(permissionId++, Modules.Identity, Actions.Manage, description: "Zarządzanie rolami i uprawnieniami"));
        permissions.Add(CreatePermission(permissionId++, Modules.Identity, "assign-roles", description: "Przypisywanie ról użytkownikom"));
        permissions.Add(CreatePermission(permissionId++, Modules.Identity, "manage-feature-flags", description: "Zarządzanie feature flags"));

        // Time
        permissions.Add(CreatePermission(permissionId++, Modules.Time, Actions.Manage, description: "Zarządzanie czasem pracy (grafiki, korekty)"));
        permissions.Add(CreatePermission(permissionId++, Modules.Time, "approve", description: "Akceptacja kart czasu pracy"));
        permissions.Add(CreatePermission(permissionId++, Modules.Time, "view-team", description: "Podgląd czasu pracy zespołu"));

        // Leave
        permissions.Add(CreatePermission(permissionId++, Modules.Leave, Actions.Manage, description: "Zarządzanie urlopami (limity, polityki)"));
        permissions.Add(CreatePermission(permissionId++, Modules.Leave, "approve", description: "Akceptacja wniosków urlopowych"));
        permissions.Add(CreatePermission(permissionId++, Modules.Leave, "view-team", description: "Podgląd kalendarza urlopów zespołu"));

        // Tasks
        permissions.Add(CreatePermission(permissionId++, Modules.Tasks, Actions.Manage, description: "Zarządzanie zadaniami (statusy, priorytety)"));
        permissions.Add(CreatePermission(permissionId++, Modules.Tasks, "assign", description: "Przypisywanie zadań użytkownikom"));

        // Workflow
        permissions.Add(CreatePermission(permissionId++, Modules.Workflow, Actions.Manage, description: "Zarządzanie definicjami workflow"));
        permissions.Add(CreatePermission(permissionId++, Modules.Workflow, "approve", description: "Akceptacja/odrzucanie kroków workflow"));

        // Dashboard
        permissions.Add(CreatePermission(permissionId++, Modules.Dashboard, Actions.Manage, description: "Konfiguracja dashboardów"));

        // Notification
        permissions.Add(CreatePermission(permissionId++, Modules.Notification, Actions.Manage, description: "Zarządzanie szablonami powiadomień"));

        // Documents
        permissions.Add(CreatePermission(permissionId++, Modules.Documents, Actions.Import, description: "Upload dokumentów"));
        permissions.Add(CreatePermission(permissionId++, Modules.Documents, Actions.Manage, description: "Zarządzanie kategoriami dokumentów"));

        // Config
        permissions.Add(CreatePermission(permissionId++, "config", Actions.Manage, description: "Zarządzanie konfiguracją systemu (wynagrodzenia, branding)"));

        return permissions;
    }

    private static Permission CreatePermission(int seed, string module, string action, string? scope = null, string? description = null)
    {
        // Generate deterministic GUID from seed
        var id = Guid.Parse($"20000000-0000-0000-0000-{seed:D12}");
        return SetId(Permission.Create(module, action, scope, description), id);
    }

    private static List<Role> CreateRoles()
    {
        return
        [
            SetId(Role.Create(DefaultTenantId, "Super Admin", RoleType.System, level: 0,
                description: "Pełny dostęp do wszystkich modułów i funkcji systemu"), SuperAdminRoleId),

            SetId(Role.Create(DefaultTenantId, "Admin", RoleType.System, level: 1,
                description: "Zarządzanie organizacją, użytkownikami i konfiguracją"), AdminRoleId),

            SetId(Role.Create(DefaultTenantId, "Kierownik", RoleType.Organizational, level: 10,
                description: "Kierownik zespołu — podgląd i akceptacja dla podległych pracowników"), KierownikRoleId),

            SetId(Role.Create(DefaultTenantId, "Pracownik", RoleType.Organizational, level: 100,
                description: "Pracownik — podstawowy dostęp do własnych danych"), PracownikRoleId),

            SetId(Role.Create(DefaultTenantId, "HR", RoleType.Organizational, level: 5,
                description: "Dział HR — zarządzanie pracownikami, urlopami i czasem pracy"), HrRoleId),
        ];
    }

    private static List<RolePermission> CreateRolePermissions(List<Permission> permissions)
    {
        var rolePermissions = new List<RolePermission>();
        var rpId = 1;

        // Super Admin — gets ALL permissions
        foreach (var permission in permissions)
        {
            rolePermissions.Add(SetId(
                RolePermission.Create(SuperAdminRoleId, permission.Id),
                Guid.Parse($"30000000-0000-0000-0000-{rpId++:D12}")));
        }

        // Admin — gets ALL permissions (full system management)
        foreach (var permission in permissions)
        {
            rolePermissions.Add(SetId(
                RolePermission.Create(AdminRoleId, permission.Id),
                Guid.Parse($"30000000-0000-0000-0000-{rpId++:D12}")));
        }

        // Kierownik — team management permissions
        var kierownikPermissions = new HashSet<string>
        {
            "org.view", "org.export",
            "time.view", "time.create", "time.view-team", "time.approve", "time.export",
            "leave.view", "leave.create", "leave.view-team", "leave.approve", "leave.export",
            "tasks.view", "tasks.create", "tasks.edit", "tasks.delete", "tasks.assign", "tasks.export",
            "workflow.view", "workflow.approve",
            "dashboard.view",
            "notification.view",
            "documents.view", "documents.create", "documents.export",
        };
        foreach (var permission in permissions.Where(p => kierownikPermissions.Contains(p.FullCode)))
        {
            rolePermissions.Add(SetId(
                RolePermission.Create(KierownikRoleId, permission.Id),
                Guid.Parse($"30000000-0000-0000-0000-{rpId++:D12}")));
        }

        // Pracownik — own data only
        var pracownikPermissions = new HashSet<string>
        {
            "org.view",
            "time.view", "time.create",
            "leave.view", "leave.create",
            "tasks.view", "tasks.edit",
            "workflow.view",
            "dashboard.view",
            "notification.view",
            "documents.view", "documents.create",
        };
        foreach (var permission in permissions.Where(p => pracownikPermissions.Contains(p.FullCode)))
        {
            rolePermissions.Add(SetId(
                RolePermission.Create(PracownikRoleId, permission.Id),
                Guid.Parse($"30000000-0000-0000-0000-{rpId++:D12}")));
        }

        // HR — broad employee/leave/time management
        var hrPermissions = new HashSet<string>
        {
            "org.view", "org.create", "org.edit", "org.delete", "org.import", "org.export", "org.manage",
            "identity.view", "identity.assign-roles",
            "time.view", "time.create", "time.edit", "time.view-team", "time.manage", "time.approve", "time.export",
            "leave.view", "leave.create", "leave.edit", "leave.delete", "leave.view-team", "leave.approve", "leave.manage", "leave.export",
            "tasks.view", "tasks.create", "tasks.export",
            "workflow.view", "workflow.approve",
            "dashboard.view", "dashboard.export",
            "notification.view",
            "documents.view", "documents.create", "documents.export", "documents.import",
        };
        foreach (var permission in permissions.Where(p => hrPermissions.Contains(p.FullCode)))
        {
            rolePermissions.Add(SetId(
                RolePermission.Create(HrRoleId, permission.Id),
                Guid.Parse($"30000000-0000-0000-0000-{rpId++:D12}")));
        }

        return rolePermissions;
    }

    private static List<DataScope> CreateDataScopes()
    {
        return
        [
            // Super Admin / Admin — Organization scope on all modules
            ..CreateDataScopesForRole(SuperAdminRoleId, DataScopeLevel.Organization, 1),
            ..CreateDataScopesForRole(AdminRoleId, DataScopeLevel.Organization, 100),

            // Kierownik — Department scope
            ..CreateDataScopesForRole(KierownikRoleId, DataScopeLevel.Department, 200),

            // Pracownik — Own scope
            ..CreateDataScopesForRole(PracownikRoleId, DataScopeLevel.Own, 300),

            // HR — Organization scope
            ..CreateDataScopesForRole(HrRoleId, DataScopeLevel.Organization, 400),
        ];
    }

    private static List<DataScope> CreateDataScopesForRole(Guid roleId, DataScopeLevel scopeLevel, int seedStart)
    {
        var scopes = new List<DataScope>();
        var idx = seedStart;
        foreach (var module in Modules.All)
        {
            scopes.Add(SetId(
                DataScope.Create(DefaultTenantId, roleId, module, scopeLevel),
                Guid.Parse($"40000000-0000-0000-0000-{idx++:D12}")));
        }
        return scopes;
    }

    private static List<FeatureFlag> CreateFeatureFlags()
    {
        var flags = new List<FeatureFlag>();
        var idx = 1;
        foreach (var module in Modules.All)
        {
            flags.Add(SetId(
                FeatureFlag.Create(DefaultTenantId, module, isEnabled: true, enabledBy: "system-seed"),
                Guid.Parse($"50000000-0000-0000-0000-{idx++:D12}")));
        }
        return flags;
    }

    /// <summary>
    /// Sets the Id on an entity using reflection (entities use private setters).
    /// </summary>
    private static T SetId<T>(T entity, Guid id) where T : class
    {
        var prop = typeof(T).GetProperty("Id")
            ?? entity.GetType().GetProperty("Id");
        prop!.SetValue(entity, id);
        return entity;
    }
}
