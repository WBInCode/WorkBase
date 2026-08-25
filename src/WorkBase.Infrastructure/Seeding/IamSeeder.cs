using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Auth;
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
        public const string Forms = "forms";
        public const string Cases = "cases";
        public const string Contacts = "contacts";
        public const string Sales = "sales";
        public const string AI = "ai";

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

    // Permission-code sets per non-admin role, shared between the initial bootstrap seeding
    // (SeedAsync, deterministic IDs for the default tenant) and on-demand tenant onboarding
    // (SeedTenantRbacAsync, random IDs for a newly created tenant) so both paths grant
    // identical baseline access.
    private static readonly HashSet<string> KierownikPermissionCodes =
    [
        "org.view", "org.export",
        // time.edit pozwala uzupelnic i poprawic ewidencje czasu wlasna oraz swojego zespolu;
        // pelny zasieg (cala firma) zostaje przy time.manage dla HR/Admina.
        "time.view", "time.create", "time.edit", "time.view-team", "time.approve", "time.export",
        "leave.view", "leave.create", "leave.view-team", "leave.approve", "leave.export",
        "tasks.view", "tasks.create", "tasks.edit", "tasks.delete", "tasks.assign", "tasks.export", "tasks.comment",
        "workflow.view", "workflow.approve",
        "dashboard.view",
        "notification.view",
        "documents.view", "documents.create", "documents.export",
        "ai.use",
        "forms.submit", "reports.view",
        "payroll.view", "payroll.view-team",
            "wnioski.view", "wnioski.create",
    ];

    private static readonly HashSet<string> PracownikPermissionCodes =
    [
        "org.view",
        "time.view", "time.create",
        "leave.view", "leave.create",
        "tasks.view", "tasks.edit", "tasks.comment",
        "workflow.view",
        "dashboard.view",
        "notification.view",
        "documents.view", "documents.create",
        "ai.use",
        "forms.submit",
        "payroll.view",
            "wnioski.view", "wnioski.create",
    ];

    private static readonly HashSet<string> HrPermissionCodes =
    [
        "org.view", "org.create", "org.edit", "org.delete", "org.import", "org.export", "org.manage",
        "identity.view",
        "time.view", "time.create", "time.edit", "time.view-team", "time.manage", "time.approve", "time.export",
        "leave.view", "leave.create", "leave.edit", "leave.delete", "leave.view-team", "leave.approve", "leave.manage", "leave.export",
        "tasks.view", "tasks.create", "tasks.export", "tasks.comment",
        "workflow.view", "workflow.approve",
        "dashboard.view", "dashboard.export",
        "notification.view",
        "documents.view", "documents.create", "documents.export", "documents.import",
        "ai.use",
        "forms.submit", "reports.view",
        "payroll.view", "payroll.view-team",
            "wnioski.view", "wnioski.create",
    ];

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        // Najpierw uzupelnij slownik uprawnien i przypisania rolom, ktore juz istnieja.
        // Musi to isc PRZED bramka ponizej, bo ta konczy metode dla kazdej dzialajacej
        // instalacji — a wlasnie tam brakowalo uprawnien dodanych po pierwszym wdrozeniu.
        await BackfillMissingPermissionsAsync(dbContext, logger);

        // Guard on ROLES, not permissions: a migration (20260512091000_AddConfigManagePermission)
        // inserts a single `config.manage` permission, so an "any permission exists" guard would
        // wrongly treat the tenant as fully seeded and skip creating the 5 system roles + grants,
        // leaving every user with 403 everywhere. Roles are the real signal that RBAC bootstrap ran.
        if (await dbContext.Set<Role>().AnyAsync(r => r.TenantId == DefaultTenantId))
        {
            logger.LogInformation("IAM roles already seeded for default tenant, skipping.");
            return;
        }

        logger.LogInformation("Seeding IAM permissions...");
        // Idempotent: skip permission codes that already exist (e.g. config.manage from a migration),
        // so re-running after a partial/half-committed seed cannot violate the unique index.
        var existingCodes = (await dbContext.Set<Permission>()
                .Select(p => new { p.Module, p.Action, p.Scope })
                .ToListAsync())
            .Select(p => p.Scope != null ? $"{p.Module}.{p.Action}.{p.Scope}" : $"{p.Module}.{p.Action}")
            .ToHashSet();

        var allPermissions = CreatePermissions();
        var newPermissions = allPermissions.Where(p => !existingCodes.Contains(p.FullCode)).ToList();
        dbContext.Set<Permission>().AddRange(newPermissions);
        await dbContext.SaveChangesAsync();

        // Re-load the full permission set (new + pre-existing) so role-permission grants below
        // reference the actual persisted rows regardless of who created them.
        var permissions = await dbContext.Set<Permission>().ToListAsync();

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
        // Idempotent: default tenant may already have feature-flag rows from a partial seed.
        var existingFlagModules = (await dbContext.Set<FeatureFlag>()
                .Where(f => f.TenantId == DefaultTenantId)
                .Select(f => f.Module)
                .ToListAsync())
            .ToHashSet();
        var featureFlags = CreateFeatureFlags().Where(f => !existingFlagModules.Contains(f.Module)).ToList();
        dbContext.Set<FeatureFlag>().AddRange(featureFlags);

        await dbContext.SaveChangesAsync();

        logger.LogInformation("IAM seeding completed: {PermCount} permissions ({NewPerm} new), {RoleCount} roles, {RpCount} role-permissions, {DsCount} data scopes, {FfCount} feature flags.",
            permissions.Count, newPermissions.Count, roles.Count, rolePermissions.Count, dataScopes.Count, featureFlags.Count);
    }

    /// <summary>
    /// Seeds the standard role set (Super Admin, Admin, Kierownik, Pracownik, HR) + their
    /// permission grants + data scopes for a NEWLY onboarded tenant. Unlike <see cref="SeedAsync"/>
    /// (which bootstraps the single default tenant once, with deterministic GUIDs), this can be
    /// called on-demand for any tenant and uses fresh random GUIDs — Role/RolePermission/DataScope
    /// are all tenant-scoped, so every tenant needs its own set of rows (permissions themselves
    /// stay global/shared, see CreatePermissions).
    ///
    /// Idempotent: no-ops if the tenant already has any Role rows.
    /// </summary>
    public static async Task SeedTenantRbacAsync(WorkBaseDbContext dbContext, Guid tenantId, ILogger logger)
    {
        if (await dbContext.Set<Role>().IgnoreQueryFilters().AnyAsync(r => r.TenantId == tenantId))
        {
            logger.LogInformation("Tenant {TenantId} already has roles seeded, skipping.", tenantId);
            return;
        }

        var permissions = await dbContext.Set<Permission>().ToListAsync();
        if (permissions.Count == 0)
        {
            logger.LogWarning("No global IAM permissions found yet — cannot seed roles for tenant {TenantId}.", tenantId);
            return;
        }

        var isOperatorTenant = tenantId == PlatformConstants.OperatorTenantId;
        var superAdminRoleId = isOperatorTenant ? Guid.NewGuid() : (Guid?)null;
        var adminRoleId = Guid.NewGuid();
        var kierownikRoleId = Guid.NewGuid();
        var pracownikRoleId = Guid.NewGuid();
        var hrRoleId = Guid.NewGuid();

        var roles = new List<Role>
        {
            SetId(Role.Create(tenantId, "Admin", RoleType.System, level: 1,
                description: "Zarządzanie organizacją, użytkownikami i konfiguracją"), adminRoleId),
            SetId(Role.Create(tenantId, "Kierownik", RoleType.Organizational, level: 10,
                description: "Kierownik zespołu — podgląd i akceptacja dla podległych pracowników"), kierownikRoleId),
            SetId(Role.Create(tenantId, "Pracownik", RoleType.Organizational, level: 100,
                description: "Pracownik — podstawowy dostęp do własnych danych"), pracownikRoleId),
            SetId(Role.Create(tenantId, "HR", RoleType.Organizational, level: 5,
                description: "Dział HR — zarządzanie pracownikami, urlopami i czasem pracy"), hrRoleId),
        };
        if (superAdminRoleId.HasValue)
        {
            roles.Insert(0, SetId(Role.Create(tenantId, "Super Admin", RoleType.System, level: 0,
                description: "Pełny dostęp do wszystkich modułów i funkcji systemu"), superAdminRoleId.Value));
        }
        dbContext.Set<Role>().AddRange(roles);
        await dbContext.SaveChangesAsync();

        var rolePermissions = new List<RolePermission>();

        // Super Admin exists only in the operator tenant and receives all permissions.
        // Customer tenants start at Admin, without platform.manage-tenants.
        if (superAdminRoleId.HasValue)
        {
            rolePermissions.AddRange(
                permissions.Select(p => RolePermission.Create(superAdminRoleId.Value, p.Id)));
        }
        rolePermissions.AddRange(permissions
            .Where(p => p.FullCode != PlatformConstants.ManageTenantsPermission)
            .Select(p => RolePermission.Create(adminRoleId, p.Id)));
        rolePermissions.AddRange(permissions.Where(p => KierownikPermissionCodes.Contains(p.FullCode)).Select(p => RolePermission.Create(kierownikRoleId, p.Id)));
        rolePermissions.AddRange(permissions.Where(p => PracownikPermissionCodes.Contains(p.FullCode)).Select(p => RolePermission.Create(pracownikRoleId, p.Id)));
        rolePermissions.AddRange(permissions.Where(p => HrPermissionCodes.Contains(p.FullCode)).Select(p => RolePermission.Create(hrRoleId, p.Id)));
        dbContext.Set<RolePermission>().AddRange(rolePermissions);

        var dataScopes = new List<DataScope>();
        if (superAdminRoleId.HasValue)
        {
            dataScopes.AddRange(Modules.All.Select(module =>
                DataScope.Create(tenantId, superAdminRoleId.Value, module, DataScopeLevel.Organization)));
        }
        dataScopes.AddRange(Modules.All.Select(module => DataScope.Create(tenantId, adminRoleId, module, DataScopeLevel.Organization)));
        dataScopes.AddRange(Modules.All.Select(module => DataScope.Create(tenantId, kierownikRoleId, module, KierownikScopeFor(module))));
        dataScopes.AddRange(Modules.All.Select(module => DataScope.Create(tenantId, pracownikRoleId, module, DataScopeLevel.Own)));
        dataScopes.AddRange(Modules.All.Select(module => DataScope.Create(tenantId, hrRoleId, module, DataScopeLevel.Organization)));
        dbContext.Set<DataScope>().AddRange(dataScopes);

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Seeded RBAC for tenant {TenantId}: {RoleCount} roles, {RpCount} role-permissions, {DsCount} data scopes.",
            tenantId, roles.Count, rolePermissions.Count, dataScopes.Count);
    }

    /// <summary>Pelne kody wszystkich uprawnien znanych systemowi (modul.akcja[.zakres]).</summary>
    public static IReadOnlyCollection<string> AllPermissionCodes { get; } =
        CreatePermissions().Select(p => p.FullCode).ToHashSet();

    /// <summary>
    /// Kod uprawnienia -> jego identyfikator. Wystawione dla testu, ktory pilnuje, zeby
    /// identyfikatory nie przesunely sie przy zmianie katalogu modulow — patrz uwaga przy
    /// <see cref="CreatePermission"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, Guid> AllPermissionIds { get; } =
        CreatePermissions().ToDictionary(p => p.FullCode, p => p.Id);

    /// <summary>
    /// Dopisuje uprawnienia, ktore pojawily sie w kodzie po pierwszym uruchomieniu instalacji,
    /// i nadaje je istniejacym rolom systemowym oraz szablonowym we wszystkich firmach.
    /// </summary>
    /// <remarks>
    /// Bez tego endpoint wymagajacy nowego uprawnienia odrzuca kazdego, lacznie z Super Adminem:
    /// nie da sie przypisac uprawnienia, ktorego nie ma w slowniku. Metoda jest idempotentna —
    /// przy braku zmian nie wykonuje zadnego zapisu.
    /// Role wlasne (utworzone przez klienta) celowo pomijamy: o ich zakresie decyduje administrator.
    /// </remarks>
    private static async Task BackfillMissingPermissionsAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        var existing = await dbContext.Set<Permission>().AsNoTracking().ToListAsync();
        var existingCodes = existing.Select(p => p.FullCode).ToHashSet();

        // Dopisywane uprawnienia dostaja SWIEZE identyfikatory. Deterministyczne numery ze
        // slownika nadaja sie tylko do pierwszego zasiewu: numeracja w dzialajacej instalacji
        // ma dziury i wartosci wstawione migracjami. Na produkcji config.manage siedzi pod
        // numerem 100, czyli dokladnie takim, jaki wypadlby jednemu z dopisywanych uprawnien —
        // przy zapisie konczylo sie to bledem sledzenia dwoch encji o tym samym kluczu i
        // aplikacja nie wstawala.
        var missing = CreatePermissions()
            .Where(permission => !existingCodes.Contains(permission.FullCode))
            .Select(permission => Permission.Create(permission.Module, permission.Action, permission.Scope, permission.Description))
            .ToList();

        if (missing.Count > 0)
        {
            dbContext.Set<Permission>().AddRange(missing);
            await dbContext.SaveChangesAsync();
            existing.AddRange(missing);
            logger.LogInformation("Uzupelniono slownik uprawnien o {Count} pozycji: {Codes}",
                missing.Count, string.Join(", ", missing.Select(p => p.FullCode)));
        }

        // Przypisania uzupelniamy zawsze, nie tylko po dodaniu uprawnienia — instalacja moze miec
        // uprawnienie w slowniku (np. z wczesniejszej migracji), ale bez nadania go rolom.
        var roles = await dbContext.Set<Role>()
            .Where(r => r.Name == "Super Admin" || r.Name == "Admin" || r.Name == "Kierownik"
                || r.Name == "Pracownik" || r.Name == "HR")
            .ToListAsync();
        if (roles.Count == 0)
        {
            return;
        }

        var roleIds = roles.Select(r => r.Id).ToList();
        var granted = (await dbContext.Set<RolePermission>()
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => new { rp.RoleId, rp.PermissionId })
                .ToListAsync())
            .Select(rp => (rp.RoleId, rp.PermissionId))
            .ToHashSet();

        var added = 0;
        foreach (var role in roles)
        {
            foreach (var permission in existing.Where(p => RoleShouldHave(role.Name, p.FullCode)))
            {
                if (granted.Add((role.Id, permission.Id)))
                {
                    dbContext.Set<RolePermission>().Add(RolePermission.Create(role.Id, permission.Id));
                    added++;
                }
            }
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Uzupelniono {Count} przypisan uprawnien do rol.", added);
        }
    }

    private static bool RoleShouldHave(string roleName, string permissionCode) => roleName switch
    {
        "Super Admin" => true,
        "Admin" => permissionCode != PlatformConstants.ManageTenantsPermission,
        "Kierownik" => KierownikPermissionCodes.Contains(permissionCode),
        "Pracownik" => PracownikPermissionCodes.Contains(permissionCode),
        "HR" => HrPermissionCodes.Contains(permissionCode),
        _ => false,
    };

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

        // Platform (operator-only, see docs/05-module-licensing-architecture.md step 5) —
        // deliberately NOT granted to Admin below, only to Super Admin, and further gated at
        // the endpoint level to require the caller's tenant to be our own operator tenant.
        permissions.Add(CreatePermission(permissionId++, "platform", "manage-tenants", description: "Zarządzanie firmami (tenantami) i ich planami licencyjnymi"));

        // Akcje spoza standardowego CRUD, których wymagają endpointy. Bez tych wpisów
        // 20 endpointów odrzucało każdego, łącznie z Super Adminem — nie da się przypisać
        // uprawnienia, którego nie ma w słowniku.
        // DOPISYWAĆ WYŁĄCZNIE NA KOŃCU: identyfikatory są wyliczane z kolejnego numeru,
        // więc wstawienie czegokolwiek wyżej przesunęłoby id już istniejących uprawnień.
        permissions.Add(CreatePermission(permissionId++, Modules.Tasks, "comment", description: "Komentowanie zadań"));

        // Ponizsze uprawnienia naleza do modulow wycofanych z ModuleCatalog 2026-08-24
        // (cases, contacts, forms, sales, ai). ZOSTAJA CELOWO: identyfikatory uprawnien sa
        // wyliczane z kolejnego numeru, wiec usuniecie ktoregokolwiek przesunieploby id
        // wszystkich nastepnych — a te sa juz zapisane w iam_role_permissions na produkcji
        // i przypisania rol wskazywalyby po cichu na inne uprawnienia.
        // Sa nieszkodliwe: zaden endpoint ich nie wymaga, a macierz uprawnien i przelaczniki
        // modulow filtruja po ModuleCatalog, wiec nie pokazuja sie w interfejsie.
        // Przy powrocie ktoregos modulu wracaja do uzycia bez zadnej zmiany tutaj.
        permissions.Add(CreatePermission(permissionId++, Modules.Cases, "comment", description: "Komentowanie spraw"));
        permissions.Add(CreatePermission(permissionId++, Modules.Cases, "assign", description: "Przypisywanie spraw"));
        permissions.Add(CreatePermission(permissionId++, Modules.Contacts, "assign", description: "Przypisywanie opiekuna kontrahenta"));
        permissions.Add(CreatePermission(permissionId++, Modules.Forms, Actions.Manage, description: "Zarządzanie definicjami formularzy"));
        permissions.Add(CreatePermission(permissionId++, Modules.Sales, Actions.Manage, description: "Zarządzanie leadami i szansami sprzedaży"));
        permissions.Add(CreatePermission(permissionId++, Modules.AI, "use", description: "Korzystanie z funkcji AI"));
        permissions.Add(CreatePermission(permissionId++, Modules.Forms, "submit", description: "Wypełnianie i wysyłanie formularzy"));

        // Wnioski firmowe (zaliczka, delegacja, praca zdalna...). Dopisane NA KONCU, bo
        // identyfikatory licza sie pozycyjnie — wstawienie wyzej przesunieploby id uprawnien
        // juz zapisanych w bazie. Patrz uwaga przy CreatePermission.
        permissions.Add(CreatePermission(permissionId++, "wnioski", Actions.View, description: "Podgląd własnych wniosków"));
        permissions.Add(CreatePermission(permissionId++, "wnioski", Actions.Create, description: "Składanie wniosków"));
        permissions.Add(CreatePermission(permissionId++, "wnioski", Actions.Manage, description: "Definiowanie rodzajów wniosków"));
        // Raporty to obszar modułu Dashboard, nie osobna pozycja katalogu modułów,
        // więc nie dostają automatycznego kompletu CRUD.
        permissions.Add(CreatePermission(permissionId++, "reports", Actions.View, description: "Przeglądanie raportów"));
        permissions.Add(CreatePermission(permissionId++, "reports", Actions.Manage, description: "Definiowanie i eksport raportów"));
        // Wynagrodzenia nie mialy zadnego uprawnienia — ekran /payroll byl w nawigacji kazdego
        // zalogowanego, a zakres danych ograniczal wylacznie DataScope.
        permissions.Add(CreatePermission(permissionId++, "payroll", Actions.View, description: "Podgląd własnego rozliczenia wynagrodzenia"));
        permissions.Add(CreatePermission(permissionId++, "payroll", "view-team", description: "Podgląd rozliczeń wynagrodzeń zespołu i firmy"));

        return permissions;
    }

    /// <summary>
    /// Identyfikator uprawnienia liczony z kolejnego numeru.
    /// </summary>
    /// <remarks>
    /// UWAGA: numeracja jest POZYCYJNA, wiec kolejnosc wywolan wyznacza identyfikatory.
    /// Petla po ModuleCatalog.All idzie pierwsza, wiec dolozenie modulu przesuwa identyfikatory
    /// WSZYSTKICH uprawnien jawnych ponizej.
    ///
    /// Na dzialajacej bazie konczy sie to kolizja klucza glownego przy starcie: seeder wstawia
    /// uprawnienia po KODZIE (pomija te, ktore juz sa), ale identyfikator bierze z licznika —
    /// a ten wskaze wiersz, ktory juz istnieje pod innym kodem. Sprawdzone na produkcji:
    /// identyfikatory 46-50 zajmuje integration.*, pozostale po module wycofanym z katalogu.
    ///
    /// Pilnuje tego IamSeederIdentyfikatoryTests. Zanim dolozysz modul do ModuleCatalog,
    /// trzeba najpierw uniezaleznic identyfikatory od pozycji.
    /// </remarks>
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

        // Admin — gets ALL permissions (full system management) except platform.manage-tenants,
        // which is reserved for Super Admin of our own operator tenant (see PlatformConstants).
        foreach (var permission in permissions.Where(p => p.FullCode != PlatformConstants.ManageTenantsPermission))
        {
            rolePermissions.Add(SetId(
                RolePermission.Create(AdminRoleId, permission.Id),
                Guid.Parse($"30000000-0000-0000-0000-{rpId++:D12}")));
        }

        // Kierownik — team management permissions
        foreach (var permission in permissions.Where(p => KierownikPermissionCodes.Contains(p.FullCode)))
        {
            rolePermissions.Add(SetId(
                RolePermission.Create(KierownikRoleId, permission.Id),
                Guid.Parse($"30000000-0000-0000-0000-{rpId++:D12}")));
        }

        // Pracownik — own data only
        foreach (var permission in permissions.Where(p => PracownikPermissionCodes.Contains(p.FullCode)))
        {
            rolePermissions.Add(SetId(
                RolePermission.Create(PracownikRoleId, permission.Id),
                Guid.Parse($"30000000-0000-0000-0000-{rpId++:D12}")));
        }

        // HR — broad employee/leave/time management
        foreach (var permission in permissions.Where(p => HrPermissionCodes.Contains(p.FullCode)))
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

            // Kierownik — Department scope (poza modulem time, patrz KierownikScopeFor)
            ..CreateDataScopesForRole(KierownikRoleId, KierownikScopeFor, 200),

            // Pracownik — Own scope
            ..CreateDataScopesForRole(PracownikRoleId, DataScopeLevel.Own, 300),

            // HR — Organization scope
            ..CreateDataScopesForRole(HrRoleId, DataScopeLevel.Organization, 400),
        ];
    }

    /// <summary>Grafik ukladany jest dla calej firmy, wiec kierownik musi go widziec w calosci.</summary>
    private static DataScopeLevel KierownikScopeFor(string module)
        => module == Modules.Time ? DataScopeLevel.Organization : DataScopeLevel.Department;

    private static List<DataScope> CreateDataScopesForRole(Guid roleId, DataScopeLevel scopeLevel, int seedStart)
        => CreateDataScopesForRole(roleId, _ => scopeLevel, seedStart);

    private static List<DataScope> CreateDataScopesForRole(Guid roleId, Func<string, DataScopeLevel> levelFor, int seedStart)
    {
        var scopes = new List<DataScope>();
        var idx = seedStart;
        foreach (var module in Modules.All)
        {
            scopes.Add(SetId(
                DataScope.Create(DefaultTenantId, roleId, module, levelFor(module)),
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
