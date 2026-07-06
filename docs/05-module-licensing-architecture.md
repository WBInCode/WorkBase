# WorkBase — Architektura licencjonowania modułów (multi-tenant, multi-realm)

> Dokument projektowy. Cel: umożliwić komercjalizację WorkBase jako platformy SaaS, gdzie każda firma-klient dostaje własny realm Keycloak, a operator platformy (my/admini) decyduje które z 15 modułów są dla niej aktywne — pojedynczo lub w pakietach (np. Bronze/Silver/Gold).
>
> Data: 2026-07-06 · Status: projekt do wdrożenia, nic z poniższego jeszcze nie zaimplementowane.

---

## 1. Model biznesowy — cel do osiągnięcia

```
Operator platformy (my)
    │
    ├── Firma A (realm Keycloak "tenant-firma-a")  → Pakiet: Silver + moduł "Sales" dokupiony
    ├── Firma B (realm Keycloak "tenant-firma-b")  → Pakiet: Bronze
    └── Firma C (realm Keycloak "tenant-firma-c")  → Pakiet: Gold (wszystkie moduły)
```

Zasady:
- **Moduły muszą działać niezależnie od siebie** — wyłączenie jednego nie może wysadzić innych.
- **Pakiety (Bronze/Silver/Gold)** to szybki punkt startowy sprzedażowy — zestaw modułów "z półki".
- **Dowolna kombinacja a la carte** musi być możliwa — klient może dokupić pojedynczy moduł spoza swojego pakietu bez zmiany całego planu.
- Włączanie/wyłączanie modułu = **decyzja operatora platformy**, wykonywana z panelu, bez zmian w kodzie i bez redeployu.

---

## 2. Stan obecny (co już działa, a co trzeba dobudować)

### ✅ Już działa i NIE wymaga przebudowy

| Element | Plik | Opis |
|---|---|---|
| Izolacja modułów (DDD) | [tests/WorkBase.Tests.Architecture/ModuleBoundaryTests.cs](../tests/WorkBase.Tests.Architecture/ModuleBoundaryTests.cs) | `NetArchTest` pilnuje braku bezpośrednich referencji między modułami. Komunikacja tylko przez `WorkBase.Contracts` + Domain Events. |
| Multi-tenancy (dane) | [src/WorkBase.Infrastructure/Persistence/WorkBaseDbContext.cs](../src/WorkBase.Infrastructure/Persistence/WorkBaseDbContext.cs) | Globalny filtr EF Core na `ITenantScoped`, automatyczna izolacja danych po `TenantId`. |
| Wymuszanie tenant_id w CQRS | `TenantBehavior.cs` (Infrastructure/Behaviors) | Każdy command/query implementujący `ITenantRequest` musi mieć poprawny `tenant_id` z JWT, inaczej `403`. |
| Tabela flag per moduł/tenant | `FeatureFlag` (Identity.Domain) | `iam_feature_flags`: unikalny indeks `(TenantId, Module)`, `IsEnabled`, `EnabledAt`, `EnabledBy`. |
| API flag | `FeatureFlagEndpoints.cs` (Identity.Api) | `GET/PUT /api/iam/feature-flags` — CRUD flag dla **własnego** tenanta. |
| UI: gate + panel | `FeatureGate.tsx`, `FeatureFlagsPage.tsx` | Chowanie sekcji UI wg flagi + panel do toggle'owania. |
| Rejestracja modułów | [src/WorkBase.Infrastructure/ModuleDiscovery.cs](../src/WorkBase.Infrastructure/ModuleDiscovery.cs) | `IModule`/`IEndpointModule` — każdy moduł sam rejestruje DI i endpointy. Wzorzec do zachowania przy nowych modułach. |

### 🔴 Krytyczne luki do zamknięcia

| # | Luka | Dlaczego to problem |
|---|---|---|
| G1 | Brak encji `Tenant` | `tenant_id` to dziś "goły" Guid bez tabeli nadrzędnej — nie ma gdzie trzymać nazwy firmy, realmu Keycloak, statusu licencji. |
| G2 | Duplikacja listy modułów w 4 miejscach | `ModuleDiscovery.cs`, `IamSeeder.Modules.All` (tylko 9/15!), `ModuleBoundaryTests.ModuleNames` (tylko 9/15!), frontend `moduleLabels`. Dodanie modułu = ryzyko zapomnienia miejsca. |
| G3 | Flagi tylko chowają UI, nie blokują API | Backend **wykonuje** żądania do wyłączonego modułu — klient bez licencji może uderzyć bezpośrednio w endpoint. |
| G4 | Brak koncepcji pakietu (Bronze/Silver/Gold) | Dziś flaga to pojedynczy bit per moduł — nie ma "zestawu startowego" do szybkiego przypisania. |
| G5 | Jeden shared realm Keycloak | Wszyscy klienci w jednym realmie `workbase`, rozróżniani atrybutem `tenant_id`. Brak izolacji realm-per-klient. |
| G6 | `IKeycloakAdminService` umie tylko CRUD userów | Brak tworzenia realmów/klientów/ról — nie da się automatycznie onboardować nowej firmy. |
| G7 | Panel flag działa tylko na "swoim" tenancie | `FeatureFlagsPage` czyta flagi z JWT aktualnego usera — brak widoku "wybierz firmę z listy" dla operatora platformy. |

---

## 3. Docelowa architektura

### 3.1 Warstwy systemu (od góry)

```
┌─────────────────────────────────────────────────────────────┐
│ Panel operatora platformy (super-admin, nowa rola)           │
│  → lista firm (Tenant) → przypisanie planu / pojedynczych    │
│    modułów → aplikacja zmian                                 │
└───────────────────────────┬───────────────────────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────────┐
│ LicensePlan (Bronze/Silver/Gold/Custom)                       │
│  → zestaw domyślnych modułów przypisywany przy onboardingu   │
└───────────────────────────┬───────────────────────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────────┐
│ FeatureFlag (TenantId × Module × IsEnabled)  ← STAN REALNY    │
│  → materializowany z planu, edytowalny a la carte             │
└───────────────────────────┬───────────────────────────────────┘
                             ▼
┌─────────────────────┬─────────────────────────────────────────┐
│ Backend enforcement  │ Frontend enforcement                    │
│ TenantBehavior sprawdza│ FeatureGate + nawigacja chowa moduł    │
│ flagę przed wykonaniem │ wyłączony (dziś już częściowo działa) │
│ handlera (403 jeśli    │                                       │
│ wyłączone) — DO ZROBIENIA│                                     │
└─────────────────────┴─────────────────────────────────────────┘
```

### 3.2 Model danych — nowe/zmienione encje

```csharp
// NOWA encja — dziś nie istnieje
public class Tenant : Entity<Guid>
{
    public string Name { get; set; }                  // "Firma A Sp. z o.o."
    public string KeycloakRealmName { get; set; }      // "tenant-firma-a"
    public Guid? LicensePlanId { get; set; }            // null = w pełni custom
    public TenantStatus Status { get; set; }            // Active / Trial / Suspended
    public DateTime CreatedAt { get; set; }
    public DateTime? TrialExpiresAt { get; set; }
}

public enum TenantStatus { Trial, Active, Suspended, Cancelled }

// NOWA encja — "szablon" pakietu
public class LicensePlan : Entity<Guid>
{
    public string Name { get; set; }                   // "Bronze" / "Silver" / "Gold"
    public string[] IncludedModules { get; set; }       // ["org","identity","time","leave","tasks",...]
    public bool IsActive { get; set; }                  // czy dostępny do sprzedaży
}

// ISTNIEJĄCA encja — bez zmian strukturalnych, ale materializowana z LicensePlan
public class FeatureFlag : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Module { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
    public string? EnabledBy { get; set; }
}
```

**Zasada działania:** `LicensePlan` to tylko "przepis" używany raz przy onboardingu (lub przy zmianie planu). Realny stan zawsze czytany jest z `FeatureFlag`. Dzięki temu:
- Przypisanie planu = jedna operacja "zastosuj przepis" (nadpisuje flagi zgodnie z planem).
- Dokupienie pojedynczego modułu = zwykły `PUT /toggle` na jednej fladze, bez dotykania planu.

### 3.3 Centralny katalog modułów (zamiast 4 duplikatów)

```csharp
// src/WorkBase.Shared/Modules/ModuleCatalog.cs — JEDYNE źródło prawdy
public static class ModuleCatalog
{
    public static readonly ModuleInfo[] All =
    [
        new("org",          "Organizacja",            ModuleGroup.Core),
        new("identity",     "Zarządzanie dostępem",   ModuleGroup.Core),
        new("time",         "Czas pracy",             ModuleGroup.Core),
        new("leave",        "Urlopy",                 ModuleGroup.Core),
        new("tasks",        "Zadania",                ModuleGroup.Core),
        new("workflow",     "Procesy",                ModuleGroup.Core),
        new("dashboard",    "Dashboard",               ModuleGroup.Core),
        new("notification", "Powiadomienia",          ModuleGroup.Core),
        new("documents",    "Dokumenty",               ModuleGroup.Standard),
        new("integration",  "Integracje",              ModuleGroup.Standard),
        new("forms",        "Formularze",              ModuleGroup.Standard),
        new("cases",        "Sprawy",                  ModuleGroup.Premium),
        new("contacts",     "Kontakty",                ModuleGroup.Premium),
        new("sales",        "Sprzedaż",                ModuleGroup.Premium),
        new("ai",           "AI",                      ModuleGroup.Premium),
    ];
}

public record ModuleInfo(string Key, string DisplayName, ModuleGroup Group);
public enum ModuleGroup { Core, Standard, Premium }
```

Konsumenci tego katalogu (po refaktorze — dziś każdy ma własną, ręczną listę):
- `IamSeeder.cs` → seeduje permissions/flagi dla wszystkich modułów z katalogu (nie tylko 9).
- `ModuleBoundaryTests.cs` → testuje izolację wszystkich modułów z katalogu.
- Frontend `FeatureFlagsPage.tsx`, `MainLayout.tsx` (nawigacja) → pobiera nazwy z tego samego źródła (przez wspólny endpoint `GET /api/iam/modules` albo statyczny plik generowany z backendu).

### 3.4 Przykładowe pakiety (nazwy robocze, do ustalenia biznesowo)

| Pakiet | Moduły | Segment klienta |
|---|---|---|
| **Bronze** (Core) | org, identity, time, leave, tasks, dashboard, notification | Małe firmy 5-30 osób — podstawowy HR/czas pracy |
| **Silver** (Core + Standard) | + workflow, documents, forms | Średnie firmy — dochodzą akceptacje i dokumentacja |
| **Gold** (wszystko) | + integration, cases, contacts, sales, ai | Duże firmy / pełny CRM+AI |

Dokupienie pojedynczego modułu spoza pakietu (np. Bronze + "Sales") — zwykła operacja na fladze, nie wymaga zmiany pakietu klienta.

---

## 4. Egzekwowanie na backendzie (najważniejsza zmiana bezpieczeństwa)

Dziś: `FeatureGate.tsx` chowa moduł w UI, ale **żaden mechanizm nie blokuje samego żądania API**. Trzeba rozszerzyć istniejący `TenantBehavior` (pipeline MediatR, uruchamiany na każdym command/query):

```csharp
public sealed class TenantBehavior<TRequest, TResponse>(
    IHttpContextAccessor httpContextAccessor,
    IFeatureFlagService featureFlagService)   // NOWA zależność
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ITenantRequest tenantRequest) return await next();

        // ...istniejąca walidacja tenant_id...

        // NOWE: sprawdzenie czy moduł, do którego należy request, jest włączony
        var moduleKey = ModuleResolver.ResolveFromRequestType(typeof(TRequest)); // np. z namespace "WorkBase.Modules.Sales.*" → "sales"
        var isEnabled = await featureFlagService.IsModuleEnabledAsync(tenantRequest.TenantId, moduleKey, ct);
        if (!isEnabled)
            return CreateFailureResult(new Error("Module.Disabled", "Moduł nie jest dostępny w Twoim planie.", ErrorType.Forbidden));

        return await next();
    }
}
```

`ModuleResolver` — prosta funkcja mapująca namespace requestu (`WorkBase.Modules.{X}.Application.Commands...`) na klucz modułu z `ModuleCatalog`. Dzięki temu **żaden handler nie musi być ręcznie oznaczony** — działa automatycznie dla wszystkich obecnych i przyszłych modułów.

---

## 5. Multi-realm Keycloak (najbardziej złożony element — na później)

### 5.1 Co trzeba dobudować w `IKeycloakAdminService`

```csharp
public interface IKeycloakAdminService
{
    // ISTNIEJĄCE
    Task<string?> CreateUserAsync(...);
    Task SetUserAttributesAsync(...);

    // NOWE — provisioning nowej firmy
    Task<string> CreateRealmAsync(string realmName, CancellationToken ct = default);
    Task CreateClientAsync(string realmName, string clientId, ClientType type, CancellationToken ct = default);
    Task CreateRealmRolesAsync(string realmName, string[] roles, CancellationToken ct = default);
    Task ImportRealmTemplateAsync(string realmName, string templateJsonPath, CancellationToken ct = default);
}
```

Onboarding nowej firmy = jedna operacja: `CreateRealmAsync` (kopia `docker/keycloak/workbase-realm.json` jako szablon) → `CreateClientAsync` dla `{realm}-web`/`{realm}-api` → `CreateRealmRolesAsync` (workbase-admin/user/kiosk) → zapis `Tenant.KeycloakRealmName`.

### 5.2 Walidacja JWT z wielu realmów (najtrudniejszy punkt technicznie)

Dziś backend waliduje token względem **jednego** `Authority` w konfiguracji. Przy realm-per-klient trzeba:
- Użyć dynamicznego `IssuerSigningKeyResolver` w `TokenValidationParameters`, który pobiera JWKS z realmu wskazanego w `iss` (issuer) tokenu, zamiast statycznego adresu.
- Trzymać mapowanie `realm name → TenantId` w tabeli `Tenant`, żeby po walidacji tokenu jednoznacznie rozpoznać firmę.
- Ograniczyć akceptowane issuery do listy realmów istniejących w tabeli `Tenant` (żeby ktoś nie podstawił dowolnego realmu).

To wymaga osobnej, dokładniejszej analizy bezpieczeństwa przed wdrożeniem — nie robić "na szybko".

---

## 6. Checklist: jak sprawnie dodać nowy (16.) moduł w przyszłości

1. Utworzyć 4 projekty: `{Module}.Domain`, `{Module}.Application`, `{Module}.Infrastructure`, `{Module}.Api` (wzorzec identyczny jak istniejące moduły).
2. Zaimplementować `IModule` (rejestracja DI) i `IEndpointModule` (mapowanie endpointów) — **bez zmian w `ModuleDiscovery.cs`** poza dopisaniem nazwy assembly do listy.
3. Dopisać **jeden wpis** do `ModuleCatalog.cs` — to automatycznie:
   - pojawia się w `IamSeeder` (permissions + domyślna flaga, wyłączona) dla wszystkich tenantów,
   - jest testowane przez `ModuleBoundaryTests`,
   - pojawia się w panelu operatora i w nawigacji frontendu (jako ukryte, dopóki flaga nie jest włączona).
4. Backfill: migracja dodająca wiersz `FeatureFlag(IsEnabled=false)` dla każdego istniejącego `Tenant`.
5. **Nie** dodawać referencji do innych modułów poza `WorkBase.Contracts` — test architektury to wyłapie od razu przy CI.

---

## 7. Plan wdrożenia (kolejność rekomendowana)

| Krok | Zakres | Ryzyko | Zależności |
|---|---|---|---|
| 1 | `ModuleCatalog.cs` + refaktor `IamSeeder`/testów architektury do korzystania z niego | Niskie | brak |
| 2 | Encja `Tenant` + migracja EF Core | Niskie | Krok 1 |
| 3 | Egzekwowanie flag na backendzie (`TenantBehavior` + `ModuleResolver`) | Średnie (dotyka pipeline wszystkich requestów) | Krok 1, 2 |
| 4 | Encja `LicensePlan` + endpoint "zastosuj plan do tenanta" | Niskie | Krok 2 |
| 5 | Panel operatora: lista firm → wybór planu/modułów (rozszerzenie `FeatureFlagsPage`) | Niskie (UI) | Krok 2, 4 |
| 6 | Multi-realm Keycloak: rozszerzenie `IKeycloakAdminService` + dynamiczna walidacja JWT | Wysokie (bezpieczeństwo, wymaga osobnego review) | Krok 2 |

Kroki 1–5 mają sens do wdrożenia i przetestowania **jeszcze na jednym, współdzielonym realmie** — nie trzeba czekać na multi-realm, żeby mieć działające zarządzanie modułami/pakietami per firma. Krok 6 to osobny, większy projekt.

---

## 8. Otwarte pytania biznesowe (do ustalenia przed implementacją)

- Dokładne nazwy i zawartość pakietów (Bronze/Silver/Gold czy inne nazewnictwo) oraz cennik.
- Czy moduł "Identity" (zarządzanie dostępem) może być kiedykolwiek wyłączony, czy zawsze wymagany (prawdopodobnie zawsze wymagany — rdzeń systemu).
- Czy potrzebny jest okres próbny (trial) z automatycznym wygaszeniem — jeśli tak, `Tenant.TrialExpiresAt` + zadanie cykliczne (Hangfire) sprawdzające wygasłe triale.
- Czy operator platformy (wy) ma być osobną rolą Keycloak (`platform-super-admin`) odróżnioną od `workbase-admin` (admin pojedynczej firmy) — rekomendacja: tak, żeby admin firmy nie mógł zarządzać flagami innych firm.
