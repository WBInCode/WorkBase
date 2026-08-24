# WorkBase — platforma operacyjno-zarządcza SaaS B2B

Czas pracy, grafiki, urlopy, zadania, obiegi akceptacji, struktura organizacyjna, dokumenty, dashboard kierowniczy. Część rodziny wb-platform, ale **inny stack i inny wzorzec auth niż reszta rodziny**.

## ZACZNIJ TUTAJ

Przed jakąkolwiek pracą przeczytaj **[`docs/ONBOARDING-AGENTA.md`](docs/ONBOARDING-AGENTA.md)** — mapa wiedzy zweryfikowana empirycznie 2026-08-24 (build, testy, produkcja, baza). Dowody i findings: **[`docs/AUDYT-2026-08-24.md`](docs/AUDYT-2026-08-24.md)**. Co robimy i w jakiej kolejności: **[`docs/PLAN-ROZWOJU-2026-08.md`](docs/PLAN-ROZWOJU-2026-08.md)**.

**Produkt jest przed pierwszym płacącym klientem.** Realne użycie to 1–4 osoby z firmy operatora (`WB Partners`); „Nowak Industries" (40 prac., 4238 wpisów) to tenant demonstracyjny wygenerowany przez `DemoDataSeeder` (`--seed-demo <guid>`). Nie myl liczb z bazy z adopcją.

`docs/archiwum/` to **drafty planistyczne z marca 2026** opisujące projekt sprzed implementacji — nie używaj ich jako obrazu rzeczywistości. Aktualne są `05` (licencjonowanie), `06` (provisioning z Huba), `07`–`09` (przewodniki użytkownika). `docs/AUDIT-KNOWLEDGE-MAP.md` (lipiec) jest częściowo nieaktualny — sprostowania w §5 nowego audytu.

## Stack — UWAGA: to jedyny projekt rodziny w .NET, nie Node

.NET 9 + ASP.NET Core Minimal API + EF Core 9 + Dapper (read-side dashboardu). React 19 + Vite + TanStack Query, **bez Tailwinda/Zustanda/Shadcn**. **Modular monolith**: 9 aktywnych modułów (Organization, Identity, TimeTracking, Leave, Tasks, Workflow, Dashboard, Notification, Documents) — patrz niżej, każdy jako 4 projekty .NET (Domain/Application/Infrastructure/Api). CQRS light przez MediatR, Domain Events in-process, multi-tenancy (shared DB + `tenant_id` + globalny query filter EF). Mobile: PWA/Capacitor. Desktop: Tauri (szkielet, 86 linii Rust). Jobs: Hangfire. Real-time: SignalR. Logi: Serilog → Seq. AV: ClamAV przy uploadzie.

Solution: `WorkBase.sln`. `src/WorkBase.{Host,Shared,Contracts,Infrastructure}`, `src/Modules/*`, `frontend/`, `src-tauri/`.

Do nawigacji po C# lepszy plugin `csharp-lsp` niż Serena (Serena celuje w TS/Python).

## Auth i ekosystem — WDROŻONE, nie „w trakcie"

Keycloak 24 (OIDC/JWT/RBAC) jako broker, Hub (wb-platform) jako źródło prawdy o firmie. Provisioning idzie webhookiem `entitlements.updated` + Instance Config API (idempotentny grant/revoke), nie handoff/redeem jak chatv2/dziennik-v2. Na produkcji włączone i działające: `Hub__Enabled=true`, SSO z EdDSA, back-channel single logout, zaproszenia pracowników, oraz integracje wychodzące do **Rytmu** (`Ecosystem__*`, job co 15 min), **czatu** (`ChatNotices__*`) i wyszukiwarki zadań (`TaskSearch__*`). Skrypty diagnostyczne: `diag-*.sh`, `configure-*.sh` w katalogu głównym. Szczegóły kontraktu: `docs/06-hub-company-provisioning.md`.

## Katalog modułów — 9, nie 15 (od 2026-08-24)

`ModuleCatalog.All` ma 9 pozycji. Sześć modułów (**integration, forms, cases, contacts, sales, ai**) zostało **wycofanych**: miały pełny kod i włączone flagi, ale żadna z ich 17 tabel nigdy nie dostała migracji, więc każde wywołanie kończyło się 500. Kod zostaje w `src/Modules/*` i nadal buduje się przez `WorkBase.sln`, ale nie jest częścią aplikacji (brak rejestracji DI, endpointów i konfiguracji EF). Powrót pojedynczego modułu: wpis w `ModuleCatalog` + `ProjectReference` w `WorkBase.Host.csproj` + migracja + uprawnienia + UI. Kolejność powrotu wg planu: integration pierwszy.

**Strażnik jest włączony:** `PendingModelChangesWarning` nie jest już wyciszany, a CI sprawdza `dotnet ef migrations has-pending-model-changes`. Jeśli start aplikacji padnie z tym ostrzeżeniem — wygeneruj migrację, nie wyciszaj.

## Wdrożenie i produkcja

Produkcja: `ssh wbvps`, `/opt/wb/workbase/`, kontenery `workbase-api` / `workbase-web` / `workbase-keycloak`, obrazy budowane **`:local` na serwerze**. Jedyna realna ścieżka wdrożenia to `deploy-scripts/deploy-prod.sh` (backup + auto-rollback). `main` = produkcja. Paczkę źródeł robi się przez `git archive` — plik `COMMIT_SHA` dostaje wtedy prawdziwy SHA (`export-subst`), a `deploy-prod.sh` porównuje go z wdrażanym commitem. Kopiowanie katalogu zamiast `git archive` bramka odrzuci.

## Infra dev

Docker w `docker/` (nie w roocie): `docker-compose.dev.yml` — Postgres 5432, Keycloak 8080, MinIO 9000/9001, Seq 5341. `.env.example` pokrywa komplet sekcji (`Hub__*`, `ChatNotices__*`, `TaskSearch__*`, `ClamAv__*`, `Integration__*`, `RateLimiting__*`, `Keycloak__Admin__*`) z komentarzem, co się dzieje przy braku wartości.

Weryfikacja: `dotnet build WorkBase.sln` (warnings-as-errors, ma przechodzić bez ostrzeżeń) · `dotnet test WorkBase.sln` (318 testów) · `cd frontend && npm run type-check && npm run lint && npm test` (55 testów).

## Konwencje

Kod dwujęzyczny: starsze partie po angielsku, nowsze (od ~lipca 2026) identyfikatory i komentarze po polsku. Trzymaj się konwencji dotykanego pliku. Uprawnienia żyją w dwóch miejscach naraz i to celowe: backend `RequirePermission("modul.akcja")` + frontend `frontend/src/auth/dostepDoWidokow.ts` (ta sama mapa steruje nawigacją i wejściem na trasę) — zmiana wymaga obu, a klucz musi istnieć w `IamSeeder.CreatePermissions`.
