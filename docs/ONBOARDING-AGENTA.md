# WorkBase — mapa wiedzy dla agenta (start nowej sesji)

> Cel: w 5 minut wiedzieć co to jest, jak działa, gdzie co leży i czego NIE zakładać.
> Zweryfikowane empirycznie 2026-08-24 (build, testy, produkcja). Stan produkcji = commit `0d98ae5` = `main`.
> Findings i plan rozwoju: [AUDYT-2026-08-24.md](AUDYT-2026-08-24.md). Poprzedni audyt (2026-07-03, częściowo nieaktualny): [AUDIT-KNOWLEDGE-MAP.md](AUDIT-KNOWLEDGE-MAP.md).

---

## 1. Czym jest WorkBase

SaaS B2B do zarządzania firmą od strony operacyjnej i kadrowej: **czas pracy, grafiki, urlopy, zadania, obiegi akceptacji, struktura organizacyjna, dokumenty, płace (stawki), dashboard kierowniczy**. Multi-tenant, licencjonowanie modułów przez feature flags (Core / Standard / Premium).

Część rodziny **wb-platform** (8 produktów, wspólny Hub SSO). **To jedyny produkt rodziny w .NET** — reszta to Node/TS. Ma też inny wzorzec auth (Keycloak jako broker przed Hubem, nie `@wb/product-sdk`).

**Realny stan użycia (produkcja, 2026-08-24) — produkt jest PRZED pierwszym płacącym klientem:** 3 firmy w bazie, ale tylko jedna żywa. `WB Partners` (firma operatora) = dogfooding, 1–4 osoby dziennie, 4–19 wpisów czasu — ostatni dziś. `Nowak Industries sp. z o.o.` = **tenant demonstracyjny**: 40 pracowników i 4238 wpisów wygenerowanych przez `DemoDataSeeder` (`--seed-demo <guid>`, `new Random(20260806)`) w minucie utworzenia tenanta; od 2026-08-14 nic. `TestowaFirma` = pusta. Nie myl liczb z bazy z adopcją — zdecydowana większość rekordów w `time_entries`, `time_schedules`, `time_sheets` i `time_anomalies` to dane demo.

---

## 2. Stack i architektura

| Warstwa | Co |
|---|---|
| Backend | .NET 9, ASP.NET Core Minimal API, EF Core 9 (+Dapper na read-side dashboardu) |
| Wzorce | Modular monolith · CQRS light (MediatR) · Result pattern · Domain Events in-process |
| Multi-tenancy | shared DB + `tenant_id` + globalny query filter EF |
| Autoryzacja | RBAC (`iam_*`) + Data Scope (zakres widoczności per rola) + `RequirePermission("modul.akcja")` na endpoincie |
| Frontend | React 19 + TS strict + Vite + TanStack Query + oidc-client-ts. **Bez** Tailwind/Zustand/Shadcn |
| DB | PostgreSQL 16 · Auth: Keycloak 24 · Storage: MinIO · AV: ClamAV · Logi: Serilog |
| Jobs | Hangfire (storage w Postgresie) · Real-time: SignalR (`/hubs/notifications`) |
| Mobile/Desktop | PWA + Capacitor 8 · Tauri 2 (`src-tauri/` — szkielet, 86 linii Rust) |

**Rozmiar:** 15 modułów, ~32 tys. linii C# w modułach + ~10 tys. w rdzeniu (`Persistence` ma 57 tys. linii, ale to w 95% migracje EF i snapshot). Frontend ~28,6 tys. linii TS/TSX w 159 plikach, 38 stron. **291 endpointów HTTP** w 54 grupach tras.

### Układ repo

```
src/WorkBase.Host/            # Program.cs + 14 plików Endpoints/ (funkcje przekrojowe, poza modułami)
src/WorkBase.Shared/          # Entity, Result, ModuleCatalog, atrybuty auth
src/WorkBase.Contracts/       # kontrakty między modułami
src/WorkBase.Infrastructure/  # Persistence (DbContext+migracje), Auth, HubPlatform, Ecosystem, Chat, Seeding, PublicApi, Middleware
src/Modules/<Nazwa>/          # 4 projekty: .Domain .Application .Infrastructure .Api
tests/                        # Unit (225) · Integration (88) · Architecture (5, NetArchTest pilnuje granic modułów)
frontend/                     # React (npm "workbase-web"), 13 plików testów Vitest (74 testy)
docker/                       # Dockerfile*, compose dev/staging, realm Keycloaka
deploy-scripts/               # deploy-prod.sh — REALNA ścieżka wdrożenia na produkcję
docs/                         # 01-06 architektura/plan, 07-09 przewodniki użytkownika, AUDYT-*
```

### ModuleCatalog — jedno źródło prawdy

`src/WorkBase.Shared/Modules/ModuleCatalog.cs` trzyma listę 15 modułów (Key, Namespace, DisplayName, Group). Konsumują go: `ModuleDiscovery` (rejestracja DI + endpointów), `IamSeeder` (uprawnienia/scope/flagi), `WorkBaseDbContext` (ładowanie konfiguracji EF z assembly modułów), `ModuleBoundaryTests`. **Dodanie modułu = jeden wpis tutaj** (+ migracja EF — patrz K1 w audycie).

| Grupa | Moduły |
|---|---|
| Core | org, identity, time, leave, tasks, workflow, dashboard, notification |
| Standard | documents, integration, forms |
| Premium | cases, contacts, sales, ai |

---

## 3. Auth i integracja z ekosystemem (WŁĄCZONA na produkcji)

Nie ma tu handoff/redeem jak w chatv2 czy dziennik-v2. Przebieg:

1. **Hub (wb-platform)** jest źródłem prawdy o firmie. `HUB org_id` ↔ `WorkBase Tenant`, `ProductInstance.id` = dostęp firmy do WorkBase.
2. Hub wysyła podpisany webhook `entitlements.updated` → WorkBase pobiera autorytatywną konfigurację z `GET /api/v1/instances/{id}/config` → idempotentny provisioning tenanta + RBAC → lista `modules` **nadpisuje lokalne feature flags 1:1**.
3. SSO: handoff JWT z Huba (`/api/hub/sso/callback`) → weryfikacja podpisu (obsługiwane też EdDSA) → sprawdzenie `instance_id → org_id` w Instance Config API → zapis `tenant_id`, `hub_org_id`, `hub_instance_id`, `hub_role` na koncie Keycloaka.
4. Mapowanie ról: HUB `owner` w firmie operatora → Super Admin; `owner` w firmie klienta → Admin; `admin`/`member` → Pracownik.
5. Back-channel single logout z Huba (`POST /api/hub/sso/logout`) zamyka sesję Keycloaka.
6. **Jedno konto Keycloak = jedna organizacja.** Osoba w wielu firmach nie jest obsługiwana.

Szczegóły: [`docs/06-hub-company-provisioning.md`](06-hub-company-provisioning.md) — ten dokument JEST aktualny.

**Integracje wychodzące (wszystkie włączone w produkcji):**

| Kanał | Config | Co robi |
|---|---|---|
| Hub | `Hub__*` | provisioning, entitlements, SSO, zaproszenia pracowników (job co minutę) |
| Rytm | `Ecosystem__*` | migawka pracownik/zadania → `wb-rytm-api:4200`, job co 15 min |
| Czat | `ChatNotices__*` | powiadomienia WorkBase trafiają na czat (`wb-chat-api:4000`) |
| Wyszukiwarka zadań | `TaskSearch__*` | `GET /api/ecosystem/tasks` dla innych aplikacji (sekret w `x-wb-task-secret`, porównanie stałoczasowe, fail-closed) |

⚠️ Rytm sync co 15 min loguje `Pomijam synchronizacje… brak konta w Rytmie` dla 3 pracowników — realnie nie synchronizuje nikogo.

---

## 4. Produkcja

```
VPS: ssh wbvps  (51.83.202.86, Debian 12)
Katalog:  /opt/wb/workbase/{docker-compose.yml, src/, COMMIT}
Kontenery: workbase-api (obraz wb-workbase-api:local)
           workbase-web (nginx + statyczny front, wb-workbase-web:local)
           workbase-keycloak (quay.io/keycloak/keycloak:24.0)
Wspólna infra: wb-postgres, wb-redis, wb-minio, wb-clamav (/opt/wb/infra)
Ruch:  Traefik (/opt/wb/proxy) → https://workbase.wb-platform.pl (stara domena wb-partners.pl w trakcie wygaszania)
Health: docker exec workbase-web wget -qO- http://workbase-api:5000/health
```

**Wdrożenie (jedyna realna ścieżka):**
```bash
git -c core.autocrlf=false -c core.eol=lf archive --format=tar.gz -o /tmp/workbase-src.tar.gz HEAD
scp /tmp/workbase-src.tar.gz wbvps:/tmp/
ssh wbvps 'bash /opt/wb/workbase/src/deploy-scripts/deploy-prod.sh <skrót-commita> [--tylko-front]'
```
Skrypt sam robi: health-check przed zmianą → `pg_dump` + tag obrazów `rollback-<ts>` → rozpakowanie źródeł → **bramka „SLADY"** (lista `plik:fraza`, którą trzeba dopisać przy każdym wdrożeniu — inaczej nie wykryje podłożenia starszej paczki) → build → up → health-check z automatycznym wycofaniem.

⚠️ `.github/workflows/cd-staging.yml` (obrazy do GHCR) i `render.yaml` (Render.com + Neon) **nie mają związku z produkcją** — obrazy budują się `:local` na VPS-ie. Nie sugeruj się nimi.

---

## 5. Uruchomienie lokalne

```bash
cp .env.example .env                                  # UWAGA: niekompletny, patrz niżej
docker compose -f docker/docker-compose.dev.yml up -d # PG 5432, Keycloak 8080, MinIO 9000/9001, Seq 5341
cd src/WorkBase.Host && dotnet run                    # API https://localhost:5001, Scalar /scalar (tylko Development)
cd frontend && npm ci && npm run dev                  # http://localhost:5173
```

Weryfikacja (wszystko przechodzi na stanie 2026-08-24):
```bash
dotnet build WorkBase.sln         # 0 błędów, 0 ostrzeżeń (warnings-as-errors w Directory.Build.props)
dotnet test WorkBase.sln          # 427 testów: 298 unit + 124 integration + 5 architecture
cd frontend && npm run type-check && npm run lint && npm test   # 0 błędów, 19 warningów ESLint, 74 testy
```

**`.env.example` nie pokrywa** sekcji obecnych w `appsettings.json` i wymaganych na produkcji: `Hub__*`, `ChatNotices__*`, `TaskSearch__*`, `ClamAv__*`, `Integration__*`, `Keycloak__Admin__*`, `RateLimiting__*`. Pełną listę kluczy zobaczysz w `src/WorkBase.Host/appsettings.json`, a realne wartości: `ssh wbvps 'docker inspect workbase-api --format "{{range .Config.Env}}{{println .}}{{end}}"'`.

Do nawigacji po C# lepszy jest plugin `csharp-lsp` niż Serena (Serena celuje w TS/Python).

---

## 6. Czego NIE zakładać (pułapki)

1. **`docs/archiwum/01`–`04` to drafty z marca 2026** (6,5 tys. linii planów sprzed implementacji). Opisują stan „0% logiki". Nie używaj ich jako obrazu rzeczywistości — używaj tego pliku i audytu. Aktualne są: `05` (licencjonowanie), `06` (provisioning z Huba), `07`–`09` (przewodniki użytkownika).
2. **6 z 15 modułów nie ma tabel w bazie** (AI, Cases, Contacts, Forms, Integration, Sales) — kod jest, DI i endpointy są zarejestrowane, feature flagi włączone, ale 17 tabel nigdy nie doczekało się migracji. Każde wywołanie = 500. Patrz K1 w audycie. **To nie jest „gotowy, opłacony kod do wpięcia"** — wbrew temu, co sugeruje audyt z lipca.
3. **Frontend nie używa Tailwind ani biblioteki komponentów.** Style to `style={{…}}` (1980 wystąpień w 78 plikach), częściowo przez zmienne CSS `var(--wb-*)` (325 użyć) zdefiniowane w `frontend/src/theme/workbase.css`. Nie „migruj do Tailwinda" mimochodem.
4. **Uprawnienia są w dwóch miejscach i to celowe:** backend `RequirePermission` na każdym endpoincie + frontend `frontend/src/auth/dostepDoWidokow.ts` (ta sama mapa steruje nawigacją i wejściem na trasę). Zmiana uprawnienia = dotknij obu. Klucze muszą istnieć w `IamSeeder.CreatePermissions`.
5. **Przełożony ≠ rola.** Akceptanta wniosku wyznacza relacja w `org_supervisor_relations`, nie rola. Stąd wyjątek `WIDOKI_DLA_PRZELOZONEGO` w `dostepDoWidokow.ts` — bez niego kolejka akceptacji znika i obieg urlopowy staje.
6. **Kolejność middleware w `Program.cs` jest wymuszona** — `UseForwardedHeaders()` MUSI iść przed `UseRateLimiter()`, inaczej cały ruch anonimowy trafia do jednej partycji limitu. Komentarze w Program.cs tłumaczą każdą taką decyzję — czytaj je przed przestawianiem.
7. **Kod i komentarze są dwujęzyczne.** Starsze partie po angielsku, nowsze (od ~lipca) identyfikatory i komentarze po polsku (`ZamknijWniosekUrlopowyPoObiegu`, `SekretPasuje`). Trzymaj się konwencji pliku, który dotykasz.
8. **~13 lokalnych gałęzi to śmieci** (0 commitów przed `main`). Kilka `+1` to duplikaty treści już wmergowanej. Nie odgrzebuj ich jako „niedokończonej pracy" bez sprawdzenia `git log main..<branch>`.
9. **Nie testuj na produkcji.** Są tam realne dane 3 firm i realna ewidencja czasu pracy 51 osób.

---

## 7. Szybkie namiary

| Czego szukasz | Gdzie |
|---|---|
| Rejestracja modułów, DI, endpointy | `src/WorkBase.Infrastructure/ModuleDiscovery.cs`, `ModuleCatalog.cs` |
| DbContext, filtr tenanta, migracje | `src/WorkBase.Infrastructure/Persistence/` |
| Uprawnienia, role, data scope | `src/WorkBase.Infrastructure/Auth/`, `Seeding/IamSeeder.cs` |
| SSO / Hub | `src/WorkBase.Infrastructure/HubPlatform/`, `src/WorkBase.Host/Endpoints/HubIntegrationEndpoints.cs` |
| Integracje ekosystemu | `src/WorkBase.Infrastructure/{Ecosystem,Chat}/`, `Host/Endpoints/EcosystemTaskEndpoints.cs` |
| Silnik obiegów | `src/Modules/Workflow/…Application/WorkflowEngine.cs` |
| Największe pliki frontu | `pages/time/SchedulePage.tsx` (1588), `TeamAttendancePage.tsx` (1075), `organization/EmployeeListPage.tsx` (899) |
| Mapa dostępu do widoków | `frontend/src/auth/dostepDoWidokow.ts` |
| Nagłówki bezpieczeństwa / CSP | `frontend/security-headers.conf` |
| Skrypty diagnostyczne Keycloak/Hub | `diag-*.sh`, `configure-*.sh` w katalogu głównym |
