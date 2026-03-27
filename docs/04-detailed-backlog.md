# WorkBase — Szczegółowy Backlog Produktu

> Dokument roboczy dla zespołu developerskiego i produktowego.
> Wersja: 0.1 | Data: 2026-03-24 | Status: Draft
> Dokumenty bazowe: 01-product-foundation.md, 02-mvp-roadmap.md, 03-technical-architecture.md

---

# 1. Epiki

## Lista epików MVP

| ID | Epik | Moduł | Faza MVP | Opis |
|---|---|---|---|---|
| E01 | Fundament infrastrukturalny | M0 | 0 | Repo, CI/CD, Docker, architektura solution, pipeline |
| E02 | Autentykacja i IAM | M0 + MOD-IDENTITY | 0 | Keycloak, JWT, login, sesje, user provisioning |
| E03 | Multi-tenancy i izolacja danych | M0 | 0 | tenant_id, global query filter, tenant resolution |
| E04 | Shared Kernel i wzorce bazowe | M0 | 0 | Entity, Result, MediatR, domain events, audyt interceptor |
| E05 | Struktura organizacyjna | MOD-ORG / M1 | 1 | Jednostki, hierarchia, pracownicy, stanowiska |
| E06 | Role i uprawnienia | MOD-IDENTITY / M2 | 1 | Definicja ról, matryca uprawnień, RBAC engine |
| E07 | Zakres widoczności danych | MOD-IDENTITY / M3 | 1 | Data scope engine, middleware, query filtering |
| E08 | Czas pracy i obecności | MOD-TIME / M4 | 2 | Clock-in/out, przerwy, timesheet, QR, grafik, anomalie |
| E09 | Workflow i akceptacje | MOD-WORKFLOW / M7 | 3 | Silnik procesów, approval flow, powiadomienia |
| E10 | Urlopy i nieobecności | MOD-LEAVE / M5 | 3 | Wnioski, limity, saldo, kalendarz |
| E11 | Zadania i egzekucja pracy | MOD-TASKS / M6 | 4 | CRUD zadań, statusy, priorytety, alerty |
| E12 | Powiadomienia | MOD-NOTIFICATION | 3 | In-app, email, szablony, SignalR |
| E13 | Dokumenty i audyt | MOD-DOCS | 0–5 | Upload plików, audyt trail, historia |
| E14 | Widok kierowniczy | MOD-DASHBOARD / M8 | 5 | Dashboard, widżety, alerty, raporty |
| E15 | Workspace użytkownika | MW | 5 | Mój dzień, moje zadania, akceptacje, feed |
| E16 | Karty rekordów 360 | MC | 5 | Karta pracownika, zespołu, zadania |
| E17 | Mobile MVP | MM | 6 | PWA/Capacitor, clock-in, QR, wnioski |
| E18 | Kiosk mode | M4 | 6 | Fullscreen web, PIN/QR identyfikacja |
| E19 | Frontend shell i design system | M0 | 0 | React shell, routing, layout, UI components |
| E20 | Background jobs i automatyzacje | M0 | 0–3 | Hangfire, joby cykliczne, alerty |

## Encje domenowe per epik

| Epik | Encje |
|---|---|
| E01 | — (infrastruktura) |
| E02 | `User`, `UserRole`, `Role` (Keycloak synced) |
| E03 | `Tenant` |
| E04 | `Entity`, `AuditableEntity`, `ValueObject`, `DomainEvent` (bazowe, shared) |
| E05 | `Tenant`, `OrganizationUnit`, `OrganizationUnitType`, `OrganizationUnitClosure`, `Position`, `Employee`, `EmployeeAssignment`, `SupervisorRelation` |
| E06 | `Role`, `Permission`, `RolePermission`, `UserRole`, `DataScope` |
| E07 | `DataScope`, `DataScopeRule` |
| E08 | `TimeEntry`, `TimeSheet`, `Schedule`, `ScheduleTemplate`, `TimeAnomaly`, `TimeCorrection`, `QrToken` |
| E09 | `WorkflowDefinition`, `WorkflowInstance`, `WorkflowStep`, `ApprovalRequest`, `ApprovalDecision`, `WorkflowAction`, `EscalationRule` |
| E10 | `LeaveType`, `LeavePolicy`, `LeaveBalance`, `LeaveRequest`, `LeaveDecision`, `LeaveCalendarEntry`, `LeaveConflict` |
| E11 | `Task`, `TaskStatus`, `TaskPriority`, `TaskStatusTransition`, `TaskComment`, `TaskAttachment`, `TaskHistory`, `TaskReminder` |
| E12 | `Notification`, `NotificationTemplate`, `NotificationPreference` |
| E13 | `Document`, `DocumentCategory`, `DocumentExpiry`, `AuditEntry` |
| E14 | `DashboardConfig`, `DashboardWidget`, `Alert`, `ReportDefinition`, `ReportExport` |
| E15 | — (agreguje dane z M4, M5, M6, M7) |
| E16 | — (generyczny renderer, custom fields: `CustomFieldDefinition`) |
| E17 | — (wrapper mobilny, natywne pluginy) |
| E18 | — (konfiguracja kiosku, web fullscreen) |
| E19 | — (frontendowe) |
| E20 | — (Hangfire joby) |

---

# 2. Feature'y per epik

## E01: Fundament infrastrukturalny

| FID | Feature | Priorytet |
|---|---|---|
| E01-F01 | Inicjalizacja repozytorium (Git, .gitignore, README) | must-have |
| E01-F02 | Solution .NET z podziałem na projekty (Host, Shared, Contracts, Modules) | must-have |
| E01-F03 | Konfiguracja PostgreSQL + EF Core + migracje | must-have |
| E01-F04 | Docker + docker-compose.dev.yml (Postgres, Keycloak, MinIO, Seq) | must-have |
| E01-F05 | CI pipeline (GitHub Actions: build, test, lint) | must-have |
| E01-F06 | Serilog structured logging + Seq sink | must-have |
| E01-F07 | Hangfire setup (PostgreSQL storage) | must-have |
| E01-F08 | MinIO/S3 file storage adapter | must-have |
| E01-F09 | Health check endpoint | must-have |
| E01-F10 | OpenAPI/Swagger auto-generation | must-have |

## E02: Autentykacja i IAM

| FID | Feature | Priorytet |
|---|---|---|
| E02-F01 | Konfiguracja Keycloak realm + client | must-have |
| E02-F02 | OIDC Authorization Code Flow + PKCE (SPA) | must-have |
| E02-F03 | JWT validation middleware (ASP.NET Core) | must-have |
| E02-F04 | Token refresh flow | must-have |
| E02-F05 | Endpoint GET /api/auth/me (current user context) | must-have |
| E02-F06 | User provisioning (Keycloak → WorkBase sync) | must-have |
| E02-F07 | Logout flow (Keycloak + frontend) | must-have |

## E03: Multi-tenancy

| FID | Feature | Priorytet |
|---|---|---|
| E03-F01 | TenantResolutionMiddleware (z JWT claim tenant_id) | must-have |
| E03-F02 | ITenantScoped interface + EF Core global query filter | must-have |
| E03-F03 | Tenant CRUD (admin: utwórz/edytuj/deaktywuj tenant) | must-have |
| E03-F04 | Seed tenant + admin user | must-have |
| E03-F05 | Feature flag registry (FeatureFlag per tenant per module) | must-have |
| E03-F06 | FeatureFlagMiddleware ([RequireModule] attribute) | must-have |

## E04: Shared Kernel

| FID | Feature | Priorytet |
|---|---|---|
| E04-F01 | Entity, AuditableEntity, ValueObject base classes | must-have |
| E04-F02 | Result<T> monad (success/failure/validation) | must-have |
| E04-F03 | ICommand / IQuery + MediatR pipeline setup | must-have |
| E04-F04 | MediatR pipeline behaviors (logging, validation, tenant) | must-have |
| E04-F05 | Domain event bus (in-process, MediatR notifications) | must-have |
| E04-F06 | PagedResult<T> + pagination helpers | must-have |
| E04-F07 | ICurrentUserService (user context z claims) | must-have |
| E04-F08 | IDataScopeContext interface | must-have |
| E04-F09 | AuditSaveChangesInterceptor (EF Core) | must-have |
| E04-F10 | FluentValidation integration + auto-discovery | must-have |
| E04-F11 | Exception handling middleware (RFC 7807) | must-have |
| E04-F12 | UUID v7 generator | must-have |

## E05: Struktura organizacyjna

| FID | Feature | Priorytet |
|---|---|---|
| E05-F01 | CRUD jednostek organizacyjnych (API + domena) | must-have |
| E05-F02 | Closure table dla hierarchii jednostek | must-have |
| E05-F03 | Konfigurowalne typy jednostek per tenant | must-have |
| E05-F04 | CRUD pracowników (profil, dane, status) | must-have |
| E05-F05 | Przypisanie pracownika do jednostki + stanowiska | must-have |
| E05-F06 | Relacja przełożony–podwładny | must-have |
| E05-F07 | CRUD stanowisk (słownik per tenant) | must-have |
| E05-F08 | GET /api/org/units/tree — drzewo hierarchii | must-have |
| E05-F09 | Import pracowników CSV | should-have |
| E05-F10 | Domain events: EmployeeCreated, AssignmentChanged, SupervisorChanged | must-have |

## E06: Role i uprawnienia

| FID | Feature | Priorytet |
|---|---|---|
| E06-F01 | CRUD ról (definicja, nazwa, opis, poziom) | must-have |
| E06-F02 | Role systemowe predefiniowane (Super Admin, Admin) | must-have |
| E06-F03 | Szablony ról startowych (Kierownik, Pracownik, HR) | must-have |
| E06-F04 | Tabela permissions (moduł + akcja + scope) | must-have |
| E06-F05 | Matryca role_permissions | must-have |
| E06-F06 | Przypisanie ról do użytkowników (user_roles) | must-have |
| E06-F07 | Permission check middleware / authorization filter | must-have |
| E06-F08 | Synchronizacja ról Keycloak ↔ WorkBase | must-have |

## E07: Zakres widoczności danych

| FID | Feature | Priorytet |
|---|---|---|
| E07-F01 | Model DataScope (reguły per rola per moduł) | must-have |
| E07-F02 | DataScopeMiddleware (wyznaczanie VisibleUnitIds) | must-have |
| E07-F03 | Automatyczne scope z pozycji w strukturze org | must-have |
| E07-F04 | Query filter integracja (EF Core / Dapper) | must-have |
| E07-F05 | Admin UI: konfiguracja scope per rola | must-have |

## E08: Czas pracy i obecności

| FID | Feature | Priorytet |
|---|---|---|
| E08-F01 | Clock-in / clock-out (API + domena) | must-have |
| E08-F02 | Przerwy start/stop (API + domena) | must-have |
| E08-F03 | QR token generation + verify API | must-have |
| E08-F04 | TimeSheet — agregat dzienny (obliczanie godzin) | must-have |
| E08-F05 | TimeSheet — widok tygodniowy/miesięczny | must-have |
| E08-F06 | Grafik/zmiana — CRUD (Schedule per pracownik per dzień) | must-have |
| E08-F07 | Schedule templates (szablony zmianowe) | should-have |
| E08-F08 | Anomaly detection — podstawowe (brak clock-out, spóźnienie, podwójne) | must-have |
| E08-F09 | Raport czasu pracy per pracownik | must-have |
| E08-F10 | Raport czasu pracy per zespół/dział | must-have |
| E08-F11 | Korekta czasu pracy przez kierownika | must-have |
| E08-F12 | Historia zdarzeń czasu pracy (timeline) | must-have |
| E08-F13 | Anomaly detection — zaawansowane (zbyt długa zmiana, praca w dzień wolny) | should-have |
| E08-F14 | Konfiguracja anomalii per tenant (progi, typy) | must-have |
| E08-F15 | Domain events: ClockInRecorded, AnomalyDetected, TimeCorrected | must-have |

## E09: Workflow i akceptacje

| FID | Feature | Priorytet |
|---|---|---|
| E09-F01 | State machine engine (generic, JSON-driven) | must-have |
| E09-F02 | WorkflowDefinition storage (JSON w bazie, wersjonowanie) | must-have |
| E09-F03 | WorkflowInstance lifecycle (create, advance, complete) | must-have |
| E09-F04 | Approval flow — single-level (jeden akceptant) | must-have |
| E09-F05 | Approval chain resolver (supervisor-of-requester rule) | must-have |
| E09-F06 | Akcje: approve / reject / return | must-have |
| E09-F07 | Automatyczne powiadomienie o oczekującej akceptacji | must-have |
| E09-F08 | Historia decyzji z timestampami | must-have |
| E09-F09 | Predefiniowane workflow: wniosek urlopowy | must-have |
| E09-F10 | Predefiniowane workflow: akceptacja zadania | must-have |
| E09-F11 | Approval wielopoziomowy (chain of approvers) | should-have |
| E09-F12 | Automatyczne eskalacje (timeout → remind / escalate) | should-have |
| E09-F13 | Domain events: WorkflowStarted, ApprovalDecisionMade, etc. | must-have |

## E10: Urlopy i nieobecności

| FID | Feature | Priorytet |
|---|---|---|
| E10-F01 | Typy nieobecności — konfigurowalna lista per tenant | must-have |
| E10-F02 | Leave policy — reguły naliczania per typ | must-have |
| E10-F03 | Leave balance — saldo per pracownik per typ per rok | must-have |
| E10-F04 | Naliczanie puli (roczne, proporcjonalne) | must-have |
| E10-F05 | Formularz wniosku urlopowego (API: POST /leave/requests) | must-have |
| E10-F06 | Integracja z workflow engine (uruchomienie approval) | must-have |
| E10-F07 | Akceptacja / odrzucenie / cofnięcie wniosku | must-have |
| E10-F08 | Historia wniosków i decyzji | must-have |
| E10-F09 | Kalendarz nieobecności (API per zespół/dział) | must-have |
| E10-F10 | Alert o konflikcie urlopowym (zbyt wielu nieobecnych) | should-have |
| E10-F11 | Załącznik do wniosku | should-have |
| E10-F12 | Domain events: LeaveRequested, LeaveApproved, BalanceUpdated | must-have |

## E11: Zadania i egzekucja pracy

| FID | Feature | Priorytet |
|---|---|---|
| E11-F01 | CRUD zadań (tytuł, opis, deadline, priorytet, status, assignee) | must-have |
| E11-F02 | Konfigurowalne statusy per tenant + przejścia | must-have |
| E11-F03 | Konfigurowalne priorytety per tenant | must-have |
| E11-F04 | Zmiana statusu z walidacją przejść (state machine) | must-have |
| E11-F05 | Komentarze do zadania | must-have |
| E11-F06 | Załączniki do zadania | must-have |
| E11-F07 | Lista zadań z filtrowaniem, sortowaniem, paginacją | must-have |
| E11-F08 | GET /tasks/my — moje zadania | must-have |
| E11-F09 | Delegowanie zadania (zmiana assignee) | should-have |
| E11-F10 | Akceptacja wykonania przez przełożonego | should-have |
| E11-F11 | Przypomnienia deadline (Hangfire job) | should-have |
| E11-F12 | Alert o opóźnieniu (termin minął) | should-have |
| E11-F13 | Historia zmian zadania | must-have |
| E11-F14 | Domain events: TaskCreated, TaskStatusChanged, TaskOverdue | must-have |

## E12: Powiadomienia

| FID | Feature | Priorytet |
|---|---|---|
| E12-F01 | Notification entity + CRUD API | must-have |
| E12-F02 | Notification dispatcher (event-driven) | must-have |
| E12-F03 | In-app notifications (API: list, unread count, mark read) | must-have |
| E12-F04 | Email transport (SMTP adapter) | must-have |
| E12-F05 | Notification templates (per typ, per język) | must-have |
| E12-F06 | SignalR hub for real-time push (in-app) | should-have |
| E12-F07 | Notification preferences per user | should-have |

## E13: Dokumenty i audyt

| FID | Feature | Priorytet |
|---|---|---|
| E13-F01 | Upload pliku (API + MinIO storage) | must-have |
| E13-F02 | Download pliku | must-have |
| E13-F03 | Lista dokumentów powiązanych z rekordem | must-have |
| E13-F04 | Soft delete dokumentu | must-have |
| E13-F05 | AuditEntry — append-only table (REVOKE UPDATE/DELETE) | must-have |
| E13-F06 | AuditSaveChangesInterceptor (auto-log zmian) | must-have |
| E13-F07 | API: GET /audit per entity/user | must-have |
| E13-F08 | Kategorie dokumentów (słownik per tenant) | should-have |
| E13-F09 | Terminy ważności dokumentów + alert | should-have |

## E14: Widok kierowniczy

| FID | Feature | Priorytet |
|---|---|---|
| E14-F01 | Dashboard API: obecność zespołu dziś | must-have |
| E14-F02 | Dashboard API: spóźnienia | must-have |
| E14-F03 | Dashboard API: zadania otwarte/zaległe per zespół | must-have |
| E14-F04 | Dashboard API: oczekujące akceptacje | must-have |
| E14-F05 | Dashboard API: anomalie czasu pracy | must-have |
| E14-F06 | Dashboard API: alerty operacyjne | must-have |
| E14-F07 | Scope dashboardu per rola (Dapper queries) | must-have |
| E14-F08 | Raport czasu pracy per zespół (API + eksport CSV) | should-have |
| E14-F09 | Raport nieobecności per zespół (API + eksport CSV) | should-have |
| E14-F10 | Alert engine (background job) | should-have |

## E15: Workspace użytkownika

| FID | Feature | Priorytet |
|---|---|---|
| E15-F01 | API: GET /workspace/my-day (agregat: czas, zadania, wnioski, akceptacje) | must-have |
| E15-F02 | Moje zadania (lista z terminami) | must-have |
| E15-F03 | Moje akceptacje (oczekujące) | must-have |
| E15-F04 | Mój czas pracy (dzisiejszy status) | must-have |
| E15-F05 | Moje wnioski (status) | must-have |
| E15-F06 | Feed aktywności (ostatnie zdarzenia) | should-have |

## E16: Karty 360

| FID | Feature | Priorytet |
|---|---|---|
| E16-F01 | Karta pracownika (dane + zespół + czas pracy + urlopy + zadania + historia) | must-have |
| E16-F02 | Karta zespołu (skład + obecność + zadania + urlopy) | must-have |
| E16-F03 | Karta zadania (dane + komentarze + historia + załączniki) | must-have |
| E16-F04 | Generyczny entity card renderer (frontend) | should-have |
| E16-F05 | Custom field definitions (CRUD per tenant per entity type) | should-have |
| E16-F06 | Custom field values (JSONB) display on cards | should-have |

## E17: Mobile MVP

| FID | Feature | Priorytet |
|---|---|---|
| E17-F01 | PWA manifest + service worker (installable) | must-have |
| E17-F02 | Capacitor project setup (Android + iOS config) | should-have |
| E17-F03 | Mobile layout (bottom tabs navigation) | must-have |
| E17-F04 | Clock-in/out z telefonu | must-have |
| E17-F05 | QR scan z kamery (Capacitor Camera plugin / web API) | must-have |
| E17-F06 | Składanie wniosku urlopowego (mobile) | should-have |
| E17-F07 | Akceptacja wniosku/zadania (quick actions) | should-have |
| E17-F08 | „Mój dzień" — uproszczony workspace | should-have |
| E17-F09 | Push notifications (FCM) | should-have |

## E18: Kiosk mode

| FID | Feature | Priorytet |
|---|---|---|
| E18-F01 | KioskLayout (fullscreen, no navigation) | should-have |
| E18-F02 | Kiosk login (systemowe konto per lokalizacja) | should-have |
| E18-F03 | PIN input identyfikacja pracownika | should-have |
| E18-F04 | QR display (rolling token) + skan QR pracownika | should-have |
| E18-F05 | Clock-in/out feedback (sukces / błąd / już zarejestrowany) | should-have |
| E18-F06 | Ostatnie odbicia (lista) | should-have |

## E19: Frontend shell i design system

| FID | Feature | Priorytet |
|---|---|---|
| E19-F01 | Vite + React 19 + TypeScript project setup | must-have |
| E19-F02 | React Router + code splitting per module | must-have |
| E19-F03 | MainLayout (sidebar + topbar + content area) | must-have |
| E19-F04 | AuthLayout (login redirect to Keycloak) | must-have |
| E19-F05 | Auth flow (OIDC, token storage, refresh, logout) | must-have |
| E19-F06 | API client (Axios/fetch wrapper, auth header, tenant) | must-have |
| E19-F07 | OpenAPI TypeScript codegen setup | must-have |
| E19-F08 | TanStack Query setup (provider, defaults) | must-have |
| E19-F09 | Zustand stores (auth, notifications) | must-have |
| E19-F10 | Design system: Button, Card, Table, Modal, Form components (Shadcn/ui) | must-have |
| E19-F11 | i18n setup (react-i18next, PL locale) | should-have |
| E19-F12 | Tailwind CSS configuration + theme tokens | must-have |
| E19-F13 | FeatureGate component (conditional module render) | must-have |
| E19-F14 | PermissionGate component (conditional per permission) | must-have |
| E19-F15 | Toast/notification system (in-app) | must-have |
| E19-F16 | Global search component (shell) | should-have |
| E19-F17 | Responsive design (desktop, tablet breakpoints) | must-have |

## E20: Background jobs

| FID | Feature | Priorytet |
|---|---|---|
| E20-F01 | TenantAwareJobFilter (Hangfire) | must-have |
| E20-F02 | Anomaly detection cron job (end-of-day check) | must-have |
| E20-F03 | Task deadline checker cron job | should-have |
| E20-F04 | Escalation timeout checker (workflow) | should-have |
| E20-F05 | Leave balance recalculation (yearly) | must-have |
| E20-F06 | Document expiry checker | should-have |
| E20-F07 | Notification cleanup (archiwizacja starych) | nice-to-have |

---

# 3. Taski szczegółowe

## Konwencja ID

```
T-{Epik}{numer_sekwencyjny}
Przykład: T-E01-001 = pierwszy task epiku E01
```

---

## EPIK E01: Fundament infrastrukturalny

### ✅ T-E01-001: Inicjalizacja repozytorium Git

| Pole | Wartość |
|---|---|
| **Nazwa** | Inicjalizacja repozytorium Git |
| **Opis** | Utworzenie repo, .gitignore (.NET + Node), README.md, CONTRIBUTING.md, branching strategy (main/feature/release) |
| **Typ** | devops |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | brak |
| **Owner** | devops / backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | Ustalić konwencję nazewnictwa branchy. GitFlow light. |
| **Status** | ✅ DONE |
| **Repo** | https://github.com/WBInCode/WorkBase |
| **Commit** | `9405fc8` — chore(init): initialize WorkBase repository |

### ✅ T-E01-002: Utworzenie solution .NET z podziałem na projekty

| Pole | Wartość |
|---|---|
| **Nazwa** | Scaffold solution .NET (modular monolith) |
| **Opis** | Utworzenie WorkBase.sln z projektami: Host, Shared, Contracts, Infrastructure, Modules/* (puste). Konfiguracja Directory.Build.props, global usings, nullable, analyzers. |
| **Typ** | architektura |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-001 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | średnie — błędna struktura na start jest kosztowna w naprawie |
| **Uwagi** | Zgodnie z 03-technical-architecture.md sekcja 4. Dodać ArchUnit test weryfikujący granice modułów. |
| **Status** | ✅ DONE |
| **Commit** | `8f56695` — arch(solution): scaffold .NET modular monolith |

### ✅ T-E01-003: Konfiguracja PostgreSQL + EF Core

| Pole | Wartość |
|---|---|
| **Nazwa** | Setup bazy danych PostgreSQL + EF Core |
| **Opis** | Konfiguracja connection string, DbContext, konfiguracja EF Core (naming convention: snake_case), pierwsza migracja (pusta), seed script. |
| **Typ** | baza danych |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-002 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Użyć Npgsql. Konwencja: snake_case tabele/kolumny. Skonfigurować od razu UUID v7 jako default PK generator. |
| **Status** | ✅ DONE |
| **Commit** | `4a3fc70` — feat(db): configure PostgreSQL + EF Core |

### ✅ T-E01-004: Docker + docker-compose.dev.yml

| Pole | Wartość |
|---|---|
| **Nazwa** | Konteneryzacja — Dockerfile + docker-compose dev |
| **Opis** | Dockerfile multi-stage dla .NET API. docker-compose.dev.yml: PostgreSQL 16, Keycloak 24, MinIO, Seq, workbase-api. Volumes, health checks. |
| **Typ** | devops |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-002, T-E01-003 |
| **Owner** | devops |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Zgodnie z 03-technical-architecture.md sekcja 14.2. Dodać .env.example. |
| **Status** | ✅ DONE |
| **Commit** | `71f8cca` — chore(docker): add Dockerfile and docker-compose.dev.yml |

### ✅ T-E01-005: CI pipeline (GitHub Actions)

| Pole | Wartość |
|---|---|
| **Nazwa** | Setup CI pipeline (build + test + lint) |
| **Opis** | GitHub Actions workflow: restore, build, test (.NET), npm ci/lint/typecheck/test/build (frontend). Trigger: push/PR do main. |
| **Typ** | devops |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-002, T-E19-001 |
| **Owner** | devops |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Dodać service container PostgreSQL do testów integracyjnych. Frontend job zakomentowany — odblokować po T-E19-001. |
| **Status** | ✅ DONE |
| **Commit** | `845d011` — ci(actions): add GitHub Actions CI workflow |

### ✅ T-E01-006: Serilog structured logging

| Pole | Wartość |
|---|---|
| **Nazwa** | Konfiguracja Serilog + Seq sink |
| **Opis** | Dodanie Serilog do Host. Console sink (dev), Seq sink (staging/prod). Enrichment: RequestId, TenantId, UserId. Request logging middleware. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-002 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | — |
| **Status** | ✅ DONE |
| **Commit** | `221f3e0` — feat(logging): add Serilog structured logging with Seq sink |

### ✅ T-E01-007: Hangfire setup

| Pole | Wartość |
|---|---|
| **Nazwa** | Konfiguracja Hangfire (PostgreSQL storage) |
| **Opis** | Dodanie Hangfire z PostgreSQL storage. Dashboard pod /hangfire (auth required). Queues: critical, default, reports. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-003 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | Hangfire dashboard dostępny tylko dla admina. Tymczasowy filtr: localhost-only (do wymiany po T-E02). |
| **Status** | ✅ DONE |
| **Commit** | `42e5715` — feat(hangfire): add Hangfire with PostgreSQL storage |

### ✅ T-E01-008: File storage adapter (MinIO)

| Pole | Wartość |
|---|---|
| **Nazwa** | Implementacja IFileStorage + adapter MinIO/S3 |
| **Opis** | Interfejs IFileStorage w Shared. Implementacja MinioFileStorage w Infrastructure. Upload, download, delete. Bucket auto-creation. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-002, T-E01-004 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | Abstrakcja pozwala na zamianę na S3/Azure Blob bez zmian w kodzie biznesowym. |
| **Status** | ✅ DONE |
| **Commit** | `c40b4d2` — feat(storage): add IFileStorage interface and MinIO adapter |

### ✅ T-E01-009: Health check endpoint

| Pole | Wartość |
|---|---|
| **Nazwa** | Health check (DB, Keycloak, MinIO, Hangfire) |
| **Opis** | ASP.NET HealthChecks: AddNpgSql, AddUrlGroup(keycloak), custom MinIO check. GET /health endpoint (bez auth). |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-003, T-E01-007 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | — |
| **Status** | ✅ DONE |
| **Commit** | `5e1f7f1` — feat(health): add health check endpoints |

### ✅ T-E01-010: OpenAPI / Swagger

| Pole | Wartość |
|---|---|
| **Nazwa** | Auto-generacja OpenAPI spec + Swagger UI |
| **Opis** | Swashbuckle / NSwag konfiguracja. Swagger UI pod /swagger (dev only). OpenAPI spec export jako artefakt CI. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-002 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | Spec jest źródłem do TypeScript codegen (T-E19-007). |
| **Status** | ✅ DONE |
| **Commit** | `eea7dbf` — feat(openapi): add OpenAPI spec generation with Scalar UI |

---

## EPIK E02: Autentykacja i IAM

### ✅ T-E02-001: Konfiguracja Keycloak realm

| Pole | Wartość |
|---|---|
| **Nazwa** | Setup Keycloak realm: workbase |
| **Opis** | Utworzenie realm config JSON: realm „workbase", client „workbase-web" (public, PKCE), client „workbase-api" (confidential). Redirect URIs, CORS, token lifetimes. Import via docker-compose. |
| **Typ** | security |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-004 |
| **Owner** | backend / devops |
| **Złożoność** | średnia |
| **Ryzyko** | średnie — Keycloak ma steep learning curve |
| **Uwagi** | Realm config w repo (IaC). Custom claim mapper dla tenant_id. Spike: warto poświęcić 1–2 dni na Keycloak POC. |
| **Status** | ✅ DONE |
| **Commit** | `4e93d40` — feat(auth): configure Keycloak realm with workbase clients |

### ✅ T-E02-002: JWT validation middleware

| Pole | Wartość |
|---|---|
| **Nazwa** | ASP.NET Core JWT bearer validation |
| **Opis** | AddAuthentication().AddJwtBearer() z Keycloak OIDC discovery. Walidacja: issuer, audience, signing key, expiry. Extraction claims: sub, tenant_id, roles, employee_id. |
| **Typ** | security |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E02-001 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | — |
| **Status** | ✅ DONE |

### ✅ T-E02-003: Frontend auth flow (OIDC PKCE)

| Pole | Wartość |
|---|---|
| **Nazwa** | Frontend: login/logout via Keycloak (OIDC PKCE) |
| **Opis** | Biblioteka oidc-client-ts lub keycloak-js. Authorization Code Flow + PKCE. Token storage (memory, nie localStorage). Refresh flow. Redirect po login. Logout z Keycloak. |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E02-001, T-E19-001 |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | średnie — PKCE flow wymaga poprawnej konfiguracji CORS/redirect |
| **Uwagi** | Token w memory (nie localStorage) — bezpieczniejsze. Refresh token via httpOnly cookie lub silent refresh. |
| **Status** | ✅ DONE |

### ✅ T-E02-004: GET /api/auth/me endpoint

| Pole | Wartość |
|---|---|
| **Nazwa** | Endpoint: current user context |
| **Opis** | Zwraca: userId, employeeId, tenantId, roles, permissions, orgUnitIds, scope level. Frontend odpytuje po login → store user context. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E02-002, T-E06-F04 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | Critical path — frontend zależy od tego endpointu. |
| **Status** | ✅ DONE |

### T-E02-005: User provisioning (Keycloak → WorkBase sync)

| Pole | Wartość |
|---|---|
| **Nazwa** | Synchronizacja użytkowników Keycloak → WorkBase |
| **Opis** | Przy pierwszym logowaniu: jeśli user istnieje w Keycloak ale nie w WorkBase → utwórz User rekord z tenant_id. Opcjonalnie: Keycloak event listener webhook. |
| **Typ** | backend / security |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E02-001, T-E02-002 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | średnie |
| **Uwagi** | Na MVP: provisioning at first login (lazy). Post-MVP: admin invite flow. |
| **Status** | ✅ DONE |

---

## EPIK E04: Shared Kernel

### T-E04-001: Base classes (Entity, AuditableEntity, ValueObject)

| Pole | Wartość |
|---|---|
| **Nazwa** | Implementacja typów bazowych Domain |
| **Opis** | Entity<TId>, AuditableEntity (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy), ValueObject, ITenantScoped. |
| **Typ** | architektura |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-002 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | Zmiany tutaj wpływają na WSZYSTKIE moduły — dobrze przemyśleć. |
| **Status** | ✅ DONE |

### T-E04-002: Result<T> monad

| Pole | Wartość |
|---|---|
| **Nazwa** | Result<T> pattern (Success/Failure/ValidationError) |
| **Opis** | Result, Result<T> z metodami: Success(), Failure(error), ValidationFailure(errors). Mapowanie na HTTP response w middleware. |
| **Typ** | architektura |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E04-001 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | — |
| **Status** | ✅ DONE |

### T-E04-003: MediatR pipeline setup

| Pole | Wartość |
|---|---|
| **Nazwa** | MediatR + pipeline behaviors |
| **Opis** | Instalacja MediatR. ICommand<TResult>, IQuery<TResult>. Pipeline behaviors: LoggingBehavior, ValidationBehavior (FluentValidation), TenantBehavior. |
| **Typ** | architektura |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E04-001 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Auto-discovery handlers i validators z modułów. |
| **Status** | ✅ DONE |

### T-E04-004: Domain event bus (in-process)

| Pole | Wartość |
|---|---|
| **Nazwa** | In-process domain event dispatching (MediatR notifications) |
| **Opis** | IDomainEvent interface. DomainEventDispatcher (po SaveChanges). Moduły mogą subskrybować eventy z innych modułów przez INotificationHandler. |
| **Typ** | architektura |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E04-003 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Fundament komunikacji między modułami. |
| **Status** | ✅ DONE |

### T-E04-005: Exception handling middleware (RFC 7807)

| Pole | Wartość |
|---|---|
| **Nazwa** | Global exception handler → Problem Details |
| **Opis** | Middleware mapujące wyjątki na RFC 7807 Problem Details. ValidationException → 400, NotFoundException → 404, UnauthorizedException → 401, ForbiddenException → 403, DomainException → 422. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-002 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | — |
| **Status** | ✅ DONE |

### T-E04-006: Audit interceptor (EF Core SaveChanges)

| Pole | Wartość |
|---|---|
| **Nazwa** | AuditSaveChangesInterceptor |
| **Opis** | EF Core SaveChangesInterceptor przechwytujący Added/Modified/Deleted na IAuditable entities. Tworzenie AuditEntry (append-only) z old/new values diff. |
| **Typ** | architektura |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E04-001, T-E01-003 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Performance: diff serializowany do JSONB. Nie logować blob/attachment content. |
| **Status** | ✅ DONE |

---

## EPIK E05: Struktura organizacyjna — backend + frontend

### T-E05-001: Tabele org_ (migracja EF Core)

| Pole | Wartość |
|---|---|
| **Nazwa** | Migracja DB: tabele struktury organizacyjnej |
| **Opis** | org_tenants, org_units, org_unit_types, org_unit_closure, org_positions, org_employees, org_employee_assignments, org_supervisor_relations. Indeksy na tenant_id, org_unit_id. |
| **Typ** | baza danych |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-003, T-E03-F02 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | średnie — closure table wymaga weryfikacji na drzewach testowych |
| **Uwagi** | **KONFIGURACJA:** org_unit_types jest słownikiem per tenant. org_unit_closure auto-populated przy CRUD. |
| **Status** | ✅ DONE |

### T-E05-002: Domain + Application layer MOD-ORG

| Pole | Wartość |
|---|---|
| **Nazwa** | Implementacja domeny: Organization Module |
| **Status** | ✅ DONE |
| **Opis** | Entities: OrganizationUnit, Employee, EmployeeAssignment, SupervisorRelation etc. Commands: CreateUnit, UpdateUnit, CreateEmployee, AssignEmployee, SetSupervisor. Queries: GetUnitTree, GetEmployees, GetEmployeeById. Validators. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E05-001, T-E04-003 |
| **Owner** | backend |
| **Złożoność** | wysoka |
| **Ryzyko** | średnie |
| **Uwagi** | Closure table rebuild na każdym Insert/Update/Delete unit. |

### T-E05-003: API controllers MOD-ORG

| Pole | Wartość |
|---|---|
| **Nazwa** | REST API: Organization endpoints |
| **Status** | ✅ DONE |
| **Opis** | OrganizationUnitsController, EmployeesController, PositionsController, UnitTypesController. Endpointy zgodnie z 03-technical-architecture.md sekcja 3.2 MOD-ORG. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E05-002 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | OpenAPI annotations na każdym endpoint. |

### T-E05-004: Frontend — widok drzewa organizacyjnego

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Drzewo organizacyjne (OrgTreePage) |
| **Status** | ✅ DONE |
| **Opis** | Interaktywne drzewo hierarchii: rozwijane/zwijane gałęzie, ikony per typ jednostki. Click → przejście do karty jednostki/zespołu. **Role: Admin, Kierownik, Dyrektor.** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E05-003, T-E19-010 |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Użyć react-arborist lub własny tree component. |

### T-E05-005: Frontend — lista i formularz pracowników

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Lista pracowników + formularz tworzenia/edycji |
| **Status** | ✅ DONE |
| **Opis** | EmployeeListPage: tabela z filtrowaniem (zespół, dział, status), search, paginacja. EmployeeForm: modal/drawer z polami (imię, nazwisko, email, stanowisko, jednostka, przełożony). **Role: Admin, HR, Kierownik.** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E05-003, T-E19-010 |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Lista respektuje data scope — kierownik widzi tylko swój zespół. |

### T-E05-006: Frontend — import CSV

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Import pracowników z CSV |
| **Status** | ✅ DONE |
| **Opis** | Upload CSV → preview (tabela z podglądem) → mapowanie kolumn → walidacja → import. Feedback: ile zaimportowano, ile błędów. |
| **Typ** | frontend |
| **Priorytet** | should-have |
| **Etap** | MVP |
| **Zależności** | T-E05-003 (API import endpoint) |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | — |

---

## EPIK E06: Role i uprawnienia — backend + frontend

### ✅ T-E06-001: Tabele iam_ (migracja)

| Pole | Wartość |
|---|---|
| **Nazwa** | Migracja DB: tabele ról i uprawnień |
| **Opis** | iam_roles, iam_permissions, iam_role_permissions, iam_user_roles, iam_data_scopes, iam_feature_flags. Seed: Super Admin, Admin roles + default permissions. |
| **Typ** | baza danych |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-003 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | średnie — matryca uprawnień wymaga starannego designu |
| **Uwagi** | Seed data musi zawierać pełną matrycę permissions per moduł per akcja. |
| **Status** | ✅ DONE |
| **Commit** | `a1ea142` — feat(identity): T-E06-001 IAM tables migration + seed data |

### ✅ T-E06-002: RBAC engine + middleware

| Pole | Wartość |
|---|---|
| **Nazwa** | Permission check middleware / authorization filter |
| **Opis** | [RequirePermission("time.view.team")] attribute na controllers/actions. AuthorizationFilter sprawdzający uprawnienia usera z JWT claims + DB lookup. |
| **Typ** | security |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E06-001, T-E02-002 |
| **Owner** | backend |
| **Złożoność** | wysoka |
| **Ryzyko** | wysokie — każdy błąd = luka bezpieczeństwa |
| **Uwagi** | Unit testy na każdą kombinację rola/moduł/akcja. Integration testy na endpointach. |
| **Status** | ✅ DONE |
| **Commit** | `9af7302` — feat(identity): T-E06-002 RBAC engine + permission middleware |

### ✅ T-E06-003: Frontend — panel ról i uprawnień

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Admin panel — zarządzanie rolami i uprawnieniami |
| **Opis** | RolesPage: lista ról, dodawanie/edycja roli. PermissionsMatrixPage: matryca checkbox (rola × moduł × akcja × scope). Przypisanie roli do usera (na karcie pracownika lub osobna lista). **Role: Admin.** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E06-001, T-E19-010 |
| **Owner** | frontend |
| **Złożoność** | wysoka |
| **Ryzyko** | średnie |
| **Uwagi** | **KONFIGURACJA:** Cała matryca edytowalna. Role systemowe (Super Admin, Admin) mają zablokowane checkboxy. |
| **Status** | ✅ DONE |

---

## EPIK E08: Czas pracy — backend + frontend

### ✅ T-E08-001: Tabele time_ (migracja)

| Pole | Wartość |
|---|---|
| **Nazwa** | Migracja DB: tabele czasu pracy |
| **Opis** | time_entries, time_sheets, time_schedules, time_schedule_templates, time_anomalies, time_corrections, time_qr_tokens. Indeksy na (tenant_id, employee_id, date). |
| **Typ** | baza danych |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-003 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | — |
| **Status** | ✅ DONE |

### T-E08-002: Domain + Application: clock-in/out, przerwy

| Pole | Wartość |
|---|---|
| **Nazwa** | Logika domenowa: rejestracja czasu pracy |
| **Opis** | TimeEntry entity z behavior (CanClockIn, CanClockOut walidacje). Commands: ClockIn, ClockOut, StartBreak, EndBreak. Business rules: nie można clock-in dwa razy, nie można break bez clock-in. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-001, T-E04-003 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | średnie — edge cases czasu pracy (północ, zmiana dnia) |
| **Uwagi** | Timezone handling — przechowywać UTC, konwertować per tenant timezone. |
| **Status** | ✅ DONE |

### T-E08-003: QR token generator + verify API

| Pole | Wartość |
|---|---|
| **Nazwa** | QR code flow: generate + verify |
| **Opis** | POST /api/time/qr/generate — generuje token z TTL (np. 30s), tenant_id, location. POST /api/time/qr/verify — weryfikuje token, rejestruje clock-in. QR token w bazie (time_qr_tokens) z expiry. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-002 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Token jednorazowy lub czas-owy (rolling). Zabezpieczenie: rate limit na verify. |
| **Status** | ✅ DONE |

### T-E08-004: TimeSheet — agregat dzienny + tygodniowy

| Pole | Wartość |
|---|---|
| **Nazwa** | TimeSheet calculation: daily/weekly/monthly |
| **Opis** | Agregacja TimeEntry → TimeSheet: suma godzin pracy, suma przerw, netto, status (complete/incomplete). API: GET /time/timesheet. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-002 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Materialized view lub on-demand calculation. |
| **Status** | ✅ DONE |
| **Commit** | `0682da0` — feat(time): T-E08-004 TimeSheet daily/weekly/monthly aggregation API |

### T-E08-005: Schedule (grafik) CRUD

| Pole | Wartość |
|---|---|
| **Nazwa** | Grafik pracy: CRUD per pracownik per dzień |
| **Opis** | Schedule entity (employee_id, date, planned_start, planned_end, shift_type). API: GET/POST/PUT /time/schedules. Schedule template (powtarzalny wzorzec). |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-001 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | **KONFIGURACJA:** Schedule templates per tenant (typy zmian). |
| **Status** | ✅ DONE |
| **Commit** | `eb676b8` — feat(time): T-E08-005 Schedule CRUD + ScheduleTemplate CRUD API |

### T-E08-006: Anomaly detection engine

| Pole | Wartość |
|---|---|
| **Nazwa** | Silnik wykrywania anomalii czasu pracy |
| **Opis** | Background job (Hangfire, koniec dnia lub real-time). Porównanie TimeEntry z Schedule. Reguły: brak clock-out, spóźnienie > próg, podwójne clock-in, zbyt długa zmiana. Tworzenie TimeAnomaly. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-002, T-E08-005, T-E01-007 |
| **Owner** | backend |
| **Złożoność** | wysoka |
| **Ryzyko** | średnie — reguły muszą być elastyczne |
| **Uwagi** | **KONFIGURACJA:** Progi anomalii per tenant (cfg_tenant_configs). Mechanizm reguł, NIE hardkodowane wartości. |
| **Status** | ✅ DONE |
| **Commit** | `b761825` — feat(time): T-E08-006 anomaly detection engine with Hangfire job |

### T-E08-007: Frontend — Clock-in/out button + status

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Przycisk clock-in/out + dzisiejszy status |
| **Opis** | ClockButton component (stan: not started / working / on break / ended). Widoczny na Workspace i w topbar. Timer live (czas od clock-in). **Role: Pracownik, Kierownik, Lider.** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-002 (API), T-E19-010 |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Współdzielony komponent (web + mobile). |
| **Status** | ✅ DONE |
| **Commit** | `f2fe262` — feat(time-ui): T-E08-007 ClockButton component with live timer |

### T-E08-008: Frontend — Timesheet (dzień/tydzień/miesiąc)

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Timesheet pracownika |
| **Opis** | TimesheetPage: widok dnia (lista wejść/wyjść/przerw), widok tygodnia (tabela per dzień), widok miesiąca (podsumowanie). Przełącznik okresów. **Role: Pracownik (swój), Kierownik (zespół).** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-004 (API) |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | — |
| **Status** | ✅ DONE |
| **Commit** | `32f9782` — feat(time-ui): T-E08-008 TimesheetPage with day/week/month views |

### T-E08-009: Frontend — Raport czasu pracy zespołu

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Raport czasu pracy per zespół (kierownik) |
| **Opis** | TeamAttendancePage: tabela pracowników × dni, godziny, anomalie (kolorowe). Filtr: data, jednostka. Eksport CSV. **Role: Kierownik, Dyrektor, HR.** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-004, T-E08-006 (API) |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | — |
| **Status** | ✅ DONE |
| **Commit** | `5bd0853` — feat(time-ui): T-E08-009 TeamAttendancePage with team grid report and CSV export |

### T-E08-010: Frontend — Grafik (widok/edycja)

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Zarządzanie grafikiem/zmianami |
| **Opis** | SchedulePage: kalendarz tygodniowy/miesięczny z przypisaniami zmian. Drag & drop lub modal edycji. **Role: Kierownik, HR, Admin.** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-005 (API) |
| **Owner** | frontend |
| **Złożoność** | wysoka |
| **Ryzyko** | średnie — UX kalendarza grafiku jest złożony |
| **Uwagi** | — |
| **Status** | ✅ DONE |
| **Commit** | `e5523be` — feat(time-ui): T-E08-010 SchedulePage with weekly/monthly calendar and modal editing |

---

## EPIK E09–E10: Workflow + Urlopy (realizowane razem w Fazie 3)

### T-E09-001: Tabele wf_ (migracja)

| Pole | Wartość |
|---|---|
| **Nazwa** | Migracja DB: tabele workflow engine |
| **Opis** | wf_definitions, wf_instances, wf_steps, wf_approval_requests, wf_approval_decisions, wf_actions, wf_escalation_rules. |
| **Typ** | baza danych |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-003 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | wf_definitions.definition_json — JSONB column. |
| **Status** | ✅ DONE |
| **Commit** | `5382923` — feat(workflow): T-E09-001 workflow engine database tables migration |

### T-E09-002: State machine engine (generic)

| Pole | Wartość |
|---|---|
| **Nazwa** | Implementacja generycznego silnika workflow |
| **Opis** | WorkflowEngine service: LoadDefinition (z JSON), CreateInstance, AdvanceStep, GetCurrentStep. State machine: walidacja dozwolonych przejść. Niezależny od typu obiektu (urlop, zadanie, sprawa). |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E09-001, T-E04-003, T-E04-004 |
| **Owner** | backend |
| **Złożoność** | wysoka |
| **Ryzyko** | wysokie — to fundament wielu modułów, musi być generyczny |
| **Uwagi** | **MECHANIZM/FRAMEWORK**, nie twarda impl. Definicja w JSON. JSON Schema validation. Testy jednostkowe na wielu scenariuszach. |
| **Status** | ✅ DONE |
| **Commit** | `5c724c2` — feat(workflow): T-E09-002 generic state machine engine with unit tests |

### T-E09-003: Approval flow — single level

| Pole | Wartość |
|---|---|
| **Nazwa** | Approval flow: jeden akceptant |
| **Opis** | ApprovalRequest entity. ApproverResolver: rule „supervisor of requester" (query do org_supervisor_relations). ApprovalDecision: approve/reject/return. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E09-002, T-E05-002 (supervisor relation) |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | średnie |
| **Uwagi** | — |

### T-E09-004: Predefiniowane workflow definitions (seed)

| Pole | Wartość |
|---|---|
| **Nazwa** | Seed: definicje workflow (urlop, akceptacja zadania) |
| **Opis** | JSON definitions seedowane przy tworzeniu tenanta. leave-request-v1: submitted → approved/rejected. task-acceptance-v1: pending → accepted/returned. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E09-002 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | **KONFIGURACJA:** Tenant admin może edytować definicje (post-MVP UI). |

### T-E10-001: Tabele leave_ (migracja)

| Pole | Wartość |
|---|---|
| **Nazwa** | Migracja DB: tabele urlopów i nieobecności |
| **Opis** | leave_types, leave_policies, leave_balances, leave_requests, leave_decisions, leave_calendar_entries. Seed: domyślne typy (urlop wypoczynkowy, na żądanie, L4, opieka). |
| **Typ** | baza danych |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-003 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | **KONFIGURACJA:** leave_types jest słownikiem edytowalnym per tenant. |

### T-E10-002: Leave domain + balance calculator

| Pole | Wartość |
|---|---|
| **Nazwa** | Logika domenowa: urlopy, saldo, naliczanie |
| **Opis** | LeaveRequest entity + walidacja (dostępne dni, brak konfliktu dat). LeaveBalance calculator (roczne, proporcjonalne do daty zatrudnienia). Commands: SubmitLeaveRequest, AdjustBalance. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E10-001, T-E09-002 (workflow integration) |
| **Owner** | backend |
| **Złożoność** | wysoka |
| **Ryzyko** | średnie — naliczanie proporcjonalne ma edge cases |
| **Uwagi** | — |

### T-E10-003: Frontend — wniosek urlopowy + kalendarz

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Formularz wniosku urlopowego + kalendarz nieobecności |
| **Opis** | LeaveRequestForm: wybór typu, dat (range picker), komentarz, submit. LeaveCalendarPage: heatmap / timeline nieobecności per zespół per miesiąc. LeaveBalanceCard: saldo per typ. **Role: Pracownik (wniosek, saldo), Kierownik (kalendarz zespołu).** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E10-002 (API), T-E09-003 (approval) |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | — |

### T-E09-005: Frontend — oczekujące akceptacje

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Lista akceptacji + akcje (approve/reject/return) |
| **Opis** | PendingApprovalsPage: lista wniosków oczekujących na moją decyzję. ApprovalActionBar: przycisk akceptuj / odrzuć / cofnij + komentarz. **Role: Kierownik, Dyrektor, HR.** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E09-003 (API) |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Widoczne też na Dashboard i w Workspace. |

---

## EPIK E11: Zadania — backend + frontend

### T-E11-001: Tabele task_ (migracja)

| Pole | Wartość |
|---|---|
| **Nazwa** | Migracja DB: tabele zadań |
| **Opis** | task_tasks, task_statuses, task_priorities, task_status_transitions, task_comments, task_attachments, task_history, task_reminders. Seed: domyślne statusy (Nowe, W toku, Do akceptacji, Zamknięte). |
| **Typ** | baza danych |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E01-003 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | **KONFIGURACJA:** Statusy i priorytety per tenant. |

### T-E11-002: Task domain + status machine

| Pole | Wartość |
|---|---|
| **Nazwa** | Logika domenowa: zadania, statusy, przejścia |
| **Opis** | Task entity + behavior (ChangeStatus z walidacją przejść, Assign, AddComment). Status machine per tenant (z task_status_transitions). Commands: CreateTask, UpdateTask, ChangeStatus, AssignTask, AddComment, AddAttachment. |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E11-001, T-E04-003 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | — |

### T-E11-003: Task overdue detector (Hangfire)

| Pole | Wartość |
|---|---|
| **Nazwa** | Background job: wykrywanie zaległych zadań |
| **Opis** | Cron job: sprawdzenie deadline < now() dla otwartych zadań. Tworzenie TaskOverdue event → powiadomienie assignee i kierownika. |
| **Typ** | backend |
| **Priorytet** | should-have |
| **Etap** | MVP |
| **Zależności** | T-E11-002, T-E01-007 |
| **Owner** | backend |
| **Złożoność** | niska |
| **Ryzyko** | niskie |
| **Uwagi** | — |

### T-E11-004: Frontend — lista zadań + karta zadania

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Lista zadań + karta szczegółów |
| **Opis** | TaskListPage: tabela z filtrowaniem (status, priorytet, assignee, overdue), sort, search, paginacja. TaskCardPage: pełne dane + komentarze + załączniki + historia + zmiana statusu. MyTasksPage: filtr na moje. **Role: Pracownik (moje), Kierownik (zespołu), Admin (global).** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E11-002 (API), T-E19-010 |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | — |

---

## EPIK E14–E15–E16: Dashboard + Workspace + Karty (Faza 5)

### T-E14-001: Dashboard backend (Dapper queries)

| Pole | Wartość |
|---|---|
| **Nazwa** | Dashboard API: agregaty operacyjne (Dapper) |
| **Opis** | Dedykowane SQL queries (Dapper): obecność dziś, spóźnienia, zadania otwarte/zaległe, oczekujące akceptacje, anomalie. Scope per rola (VisibleUnitIds). |
| **Typ** | backend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E08-002, T-E10-002, T-E11-002, T-E09-003, T-E07-F02 |
| **Owner** | backend |
| **Złożoność** | średnia |
| **Ryzyko** | średnie — performance zależy od indeksów i wolumenu |
| **Uwagi** | Read-only. Nie przechodzi przez EF Core (Dapper bezpośrednie SQL). |

### T-E14-002: Frontend — DashboardPage

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Dashboard kierowniczy |
| **Opis** | DashboardPage: grid widżetów: AttendanceWidget (obecni/nieobecni/spóźnieni), TaskSummaryWidget (otwarte/zaległe), PendingApprovalsWidget (ile czeka), AlertsWidget (anomalie, eskalacje). Scope per rola. **Role: Kierownik, Dyrektor, Zarząd, HR.** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E14-001, T-E19-010 |
| **Owner** | frontend |
| **Złożoność** | wysoka |
| **Ryzyko** | średnie — to main selling point — UX musi być doskonały |
| **Uwagi** | Widżety jako oddzielne komponenty — gotowe pod przyszłą konfigurowalność. |

### T-E15-001: Frontend — WorkspacePage (Mój dzień)

| Pole | Wartość |
|---|---|
| **Nama** | UI: Workspace — co mam dziś do zrobienia |
| **Opis** | WorkspacePage: MyDayOverview (clock status), MyTasksList (otwarte, zbliżające się deadline'y), MyApprovalsWidget (oczekujące decyzje), MyLeaveWidget (moje wnioski), ActivityFeed (ostatnie zdarzenia). **Role: Pracownik, Kierownik (każdy).** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E15-F01 (API), T-E08-007, T-E19-010 |
| **Owner** | frontend |
| **Złożoność** | średnia |
| **Ryzyko** | niskie |
| **Uwagi** | Strona startowa po logowaniu (default route). |

### T-E16-001: Frontend — Karta pracownika 360

| Pole | Wartość |
|---|---|
| **Nazwa** | UI: Karta pracownika (dane, czas pracy, urlopy, zadania, historia) |
| **Opis** | EmployeeCardPage: sekcje (dane osobowe, zespół/stanowisko, timesheet bieżący, saldo urlopów, ostatnie zadania, timeline aktywności). **Role: Kierownik (zespół), HR (global), Admin.** |
| **Typ** | frontend |
| **Priorytet** | must-have |
| **Etap** | MVP |
| **Zależności** | T-E05-003, T-E08-004, T-E10-002, T-E11-002, T-E13-F07 |
| **Owner** | frontend |
| **Złożoność** | wysoka |
| **Ryzyko** | niskie |
| **Uwagi** | Agreguje dane z wielu modułów. |

---

# 4. Subtaski (przykładowe rozbicie wybranych tasków)

## T-E09-002: State machine engine — subtaski

| SID | Subtask | Owner |
|---|---|---|
| T-E09-002-a | Definicja JSON Schema dla WorkflowDefinition | backend |
| T-E09-002-b | Parser JSON → in-memory workflow graph | backend |
| T-E09-002-c | WorkflowInstance lifecycle (create, init step, advance) | backend |
| T-E09-002-d | Transition validator (dozwolone przejścia + reguły) | backend |
| T-E09-002-e | Action executor (notify, create_task, update_entity) | backend |
| T-E09-002-f | Unit tests: happy path (submit → approve → done) | backend |
| T-E09-002-g | Unit tests: edge cases (reject, return, re-submit) | backend |
| T-E09-002-h | Integration test: leave request workflow end-to-end | backend |

## T-E08-006: Anomaly detection — subtaski

| SID | Subtask | Owner |
|---|---|---|
| T-E08-006-a | IAnomalyRule interface + rule executor | backend |
| T-E08-006-b | Rule: MissingClockOutRule | backend |
| T-E08-006-c | Rule: LateArrivalRule (porównanie z Schedule) | backend |
| T-E08-006-d | Rule: DoubleClockInRule | backend |
| T-E08-006-e | Rule: OverlongShiftRule (konfigurowalny próg) | backend |
| T-E08-006-f | Konfiguracja progów per tenant (cfg_tenant_configs) | backend |
| T-E08-006-g | Hangfire job: EndOfDayAnomalyCheck | backend |
| T-E08-006-h | Event: AnomalyDetected → notify manager | backend |
| T-E08-006-i | Anomaly list API + review/dismiss endpoints | backend |

## T-E02-003: Frontend auth flow — subtaski

| SID | Subtask | Owner |
|---|---|---|
| T-E02-003-a | Instalacja i konfiguracja oidc-client-ts | frontend |
| T-E02-003-b | AuthProvider context (React) | frontend |
| T-E02-003-c | Login redirect → Keycloak | frontend |
| T-E02-003-d | Callback handler (token parsing, storage) | frontend |
| T-E02-003-e | Token refresh (silent renew) | frontend |
| T-E02-003-f | Logout (clear tokens, redirect) | frontend |
| T-E02-003-g | ProtectedRoute component (redirect if unauthenticated) | frontend |
| T-E02-003-h | useAuth hook (isAuthenticated, user, token) | frontend |

---

# 5. Absolutne minimum MVP

> Bez tych elementów system nie ma prawa istnieć — jest bezużyteczny.

| # | Element | Epik | Uzasadnienie |
|---|---|---|---|
| 1 | Multi-tenancy z izolacją danych | E03 | Bez izolacji nie można uruchomić żadnego klienta — fundament SaaS |
| 2 | Autentykacja (login, JWT, sesja) | E02 | Bez logowania nie ma użytkowników |
| 3 | Struktura organizacyjna (jednostki + pracownicy) | E05 | Bez pracowników nie ma komu rejestrować czasu, urlopów, zadań |
| 4 | Hierarchia przełożony–podwładny | E05 | Bez niej nie działają akceptacje, delegowanie, widoczność danych |
| 5 | Role i uprawnienia (RBAC) | E06 | Bez ról każdy widzi wszystko — nie do przyjęcia w firmie |
| 6 | Data scope (zakres widoczności) | E07 | Kierownik musi widzieć TYLKO swój zespół — bez tego brak sensu |
| 7 | Clock-in / clock-out | E08 | Rejestracja obecności to główny przypadek użycia MVP |
| 8 | Timesheet (dzienny / tygodniowy) | E08 | Bez niego dane z clock-in nie są użyteczne |
| 9 | Workflow — single-level approval | E09 | Bez akceptacji wnioski urlopowe/zadania nie mają flow |
| 10 | Wniosek urlopowy + saldo | E10 | Drugi główny przypadek użycia MVP |
| 11 | Powiadomienia (in-app minimum) | E12 | Bez powiadomień nikt nie wie o oczekującej akceptacji |

**Jeśli brakuje choćby jednego z punktów 1–11, system jest bezużyteczny jako produkt operacyjny.**

**Warunkowe minimum (must-have ale z opóźnieniem dopuszczalnym):**
- Zadania (E11) — mogą wejść 2–4 tygodnie po uruchomieniu
- Dashboard kierowniczy (E14) — kierownicy mogą początkowo korzystać z list
- Mobile (E17) — PWA wystarczy jako pierwszy krok

---

# 6. Główne API / use-case'y per moduł

| Moduł | Endpoint prefix | Główne API (resources + key operations) |
|---|---|---|
| **MOD-ORG** | `/api/org/` | `units` (CRUD, tree), `employees` (CRUD, assign, import), `positions` (CRUD), `unit-types` (CRUD) |
| **MOD-IDENTITY** | `/api/auth/`, `/api/roles/` | `auth/me` (current user), `roles` (CRUD), `permissions` (matrix), `user-roles` (assign), `data-scopes` (CRUD), `feature-flags` (toggle) |
| **MOD-TIME** | `/api/time/` | `entries` (clock-in, clock-out, break), `timesheets` (daily/weekly/monthly), `schedules` (CRUD, templates), `anomalies` (list, dismiss), `corrections` (CRUD), `qr` (generate, verify) |
| **MOD-LEAVE** | `/api/leave/` | `types` (CRUD), `policies` (CRUD), `balances` (get, adjust), `requests` (submit, cancel), `calendar` (team view), `conflicts` (check) |
| **MOD-TASKS** | `/api/tasks/` | `tasks` (CRUD, change-status, assign, my), `statuses` (CRUD + transitions), `priorities` (CRUD), `comments` (CRUD), `attachments` (upload, download) |
| **MOD-WORKFLOW** | `/api/workflow/` | `definitions` (CRUD, versions), `instances` (create, advance), `approvals` (pending-for-me, approve, reject, return), `escalations` (config) |
| **MOD-DASHBOARD** | `/api/dashboard/` | `attendance-today`, `late-arrivals`, `open-tasks`, `pending-approvals`, `anomalies`, `alerts`, `reports/export` |
| **MOD-NOTIFICATION** | `/api/notifications/` | `notifications` (list, unread-count, mark-read, mark-all-read), `templates` (CRUD), `preferences` (get, update), SignalR hub: `/hubs/notifications` |
| **MOD-DOCS** | `/api/documents/` | `documents` (upload, download, list, soft-delete), `categories` (CRUD), `audit` (per-entity, per-user) |

---

# 7. Taski UX / Design

| ID | Nazwa | Opis | Priorytet | Etap | Złożoność | Zależności |
|---|---|---|---|---|---|---|
| T-UX-001 | Design system — tokens i paleta kolorów | Definicja kolorów (primary, secondary, semantic), typografii, spacingów, border-radius. Tailwind theme config. | must-have | MVP | średnia | — |
| T-UX-002 | Wireframes — MainLayout (sidebar + topbar) | Layout nawigacji: sidebar (zwijany), topbar (user, notifications, clock), content area. Wersja desktop + tablet. | must-have | MVP | średnia | — |
| T-UX-003 | Wireframes — Workspace (Mój dzień) | Strona startowa: clock status, moje zadania, moje akceptacje, mój czas, feed. Mobile-first. | must-have | MVP | średnia | — |
| T-UX-004 | Wireframes — Dashboard kierowniczy | Grid widżetów: obecność, spóźnienia, zadania, akceptacje, alerty. Responsywny grid. | must-have | MVP | średnia | — |
| T-UX-005 | Wireframes — Timesheet (dzień/tydzień/miesiąc) | Widok timeline dnia, tabela tygodnia, podsumowanie miesiąca. Kolorystyka statusów. | must-have | MVP | średnia | — |
| T-UX-006 | Wireframes — Formularz wniosku urlopowego | Date range picker, typ nieobecności, saldo, komentarz, submit. Modal/drawer. | must-have | MVP | niska | — |
| T-UX-007 | Wireframes — Karta pracownika 360 | Sekcje: dane, zespół, czas pracy dzisiejszy, saldo urlopów, zadania, historia. Tab navigation. | must-have | MVP | średnia | — |
| T-UX-008 | Wireframes — Drzewo organizacyjne | Interaktywne drzewo: rozwijanie/zwijanie, ikony typów, click-through do karty. | should-have | MVP | niska | — |
| T-UX-009 | Wireframes — Lista zadań + karta zadania | Tabela z filtrami, status badges, priority indicators. Karta: timeline zmian, komentarze, attachmenty. | must-have | MVP | średnia | — |
| T-UX-010 | Wireframes — Grafik pracy (calendar view) | Widok tygodniowy, kolorowe shift-bars, drag-to-assign. | should-have | MVP | wysoka | — |
| T-UX-011 | Wireframes — Akceptacje (lista + action bar) | Lista wniosków, preview detali, approve/reject/return buttons, komentarz. | must-have | MVP | niska | — |
| T-UX-012 | Wireframes — Admin: matryca uprawnień | Checkbox matrix (role × modules × actions). Grouping, batch edit. | should-have | MVP | średnia | — |
| T-UX-013 | Mobile wireframes (bottom tab layout) | Bottom tabs: Mój dzień, Zadania, Wnioski, Więcej. Uproszczony layout. | must-have | MVP | średnia | — |
| T-UX-014 | Kiosk wireframes (fullscreen clock-in) | Large clock button, PIN input, QR display, success/error feedback. Large touch targets. | should-have | MVP | niska | — |
| T-UX-015 | Icon set + status color system | Definicja ikon per moduł, status badges (colors + shapes), accessibility (WCAG AA). | should-have | MVP | niska | — |
| T-UX-016 | Loading/empty/error state patterns | Skeleton loaders, empty states (ilustracja + CTA), error states (retry). Spójne w całej aplikacji. | must-have | MVP | niska | — |
| T-UX-017 | Notification UX: bell icon + dropdown + toast | Bell z badge count, dropdown lista, real-time toast on new notification. | must-have | MVP | niska | T-FE-024 |

---

# 8. Zależności między epikami

```
E01 (Fundament)
 ├── E02 (Auth) ─────────────────────────────────────────┐
 ├── E03 (Multi-tenancy) ───────────────────────────────┐│
 ├── E04 (Shared Kernel) ─────────────────────────────┐ ││
 ├── E19 (Frontend shell) ───────────────────────────┐ │ ││
 │                                                    │ │ ││
 │   E05 (Org) ←── E04, E03                         │ │ ││
 │    │                                               │ │ ││
 │    ├── E06 (Role) ←── E04, E02                    │ │ ││
 │    │    │                                          │ │ ││
 │    │    └── E07 (Scope) ←── E06, E05              │ │ ││
 │    │         │                                     │ │ ││
 │    ├─────────┼── E08 (Time) ←── E05, E07          │ │ ││
 │    │         │    │                                │ │ ││
 │    │         │    │                                │ │ ││
 │    ├─────────┼── E09 (Workflow) ←── E05, E06      │ │ ││
 │    │         │    │                                │ │ ││
 │    │         │    ├── E10 (Leave) ←── E09          │ │ ││
 │    │         │    │                                │ │ ││
 │    ├─────────┼── E11 (Tasks) ←── E05, E07         │ │ ││
 │    │         │                                     │ │ ││
 │    │         │                                     │ │ ││
 │    └── E12 (Notifications) ←── E04, E01           │ │ ││
 │         │                                          │ │ ││
 │    E13 (Docs/Audit) ←── E04, E01                  │ │ ││
 │         │                                          │ │ ││
 │    E14 (Dashboard) ←── E08, E10, E11, E09, E07    │ │ ││
 │    E15 (Workspace) ←── E08, E10, E11, E09         │ │ ││
 │    E16 (Karty 360) ←── E05, E08, E10, E11         │ │ ││
 │         │                                          │ │ ││
 │    E17 (Mobile) ←── E08, E10, E11, E19            │ │ ││
 │    E18 (Kiosk) ←── E08, E19                       │ │ ││
 │                                                    │ │ ││
 └── E20 (Jobs) ←── E01, E08, E10, E11, E09         │ │ ││
```

---

# 9. Krytyczna ścieżka MVP

**Najdłuższy łańcuch zależności:**

```
E01 → E04 → E03 → E05 → E06 → E07 → E09 → E10 → E14 → E17
 │                                     ↗                    ↗
 └──── E02 ──────────────────────────┘                    │
 │                                                         │
 └──── E19 ──────────────────────────────────────────────┘
```

**Kolejność krytyczna (nie można zrównoleglić):**

1. E01 (repo, docker, DB, logs)
2. E04 (shared kernel — entity, result, MediatR, audit) **+ E02 (Keycloak) — parallel**
3. E03 (multi-tenancy) **+ E19 (frontend shell) — parallel**
4. E05 (org structure)
5. E06 (roles/permissions) + E07 (data scope)
6. **FORK:** E08 (time) || E11 (tasks) || E09 (workflow) — **równolegle 3 strumienie**
7. E10 (leave) — po E09
8. E12 (notifications) — po E09
9. E14 (dashboard) + E15 (workspace) + E16 (karty) — po E08, E10, E11
10. E17 (mobile) + E18 (kiosk) — po E08

**Wnioski:**
- Backend i frontend mogą pracować równolegle od Fazy 1 (E05 backend + E19 frontend shell)
- Fazy 2 i 4 (czas pracy + zadania) mogą iść równolegle
- Workflow engine (E09) musi wystartować w Fazie 3 najpóźniej — jest na krytycznej ścieżce do urlopów

---

# 10. Taski architektoniczne

| ID | Nazwa | Opis | Priorytet | Etap | Złożoność |
|---|---|---|---|---|---|
| T-ARCH-001 | Scaffold modular monolith solution | Struktura .NET: Host, Shared, Contracts, Modules | must-have | MVP | średnia |
| T-ARCH-002 | Module registration pattern | IModule interface, auto-discovery, DI registration per module | must-have | MVP | średnia |
| T-ARCH-003 | ArchUnit boundary tests | Testy weryfikujące: moduł A nie importuje typów modułu B | must-have | MVP | niska |
| T-ARCH-004 | Domain event bus (in-process) | MediatR INotification dispatching between modules | must-have | MVP | średnia |
| T-ARCH-005 | CQRS light: command/query separation | ICommand, IQuery, osobne handlersy | must-have | MVP | niska |
| T-ARCH-006 | Specification pattern base | ISpecification<T> for complex queries | should-have | MVP | niska |
| T-ARCH-007 | Tenant config system | cfg_tenant_configs table + ITenantConfigService | must-have | MVP | średnia |
| T-ARCH-008 | Custom fields infrastructure | CustomFieldDefinition table + JSONB storage + query | should-have | MVP | wysoka |

---

# 11. Taski backendowe (agregacja)

| ID | Nazwa | Epik | Priorytet | Etap | Złożoność |
|---|---|---|---|---|---|
| T-BE-001 | Organization module: domain + app + API | E05 | must-have | MVP | wysoka |
| T-BE-002 | Identity module: roles, permissions, RBAC engine | E06 | must-have | MVP | wysoka |
| T-BE-003 | Data scope engine | E07 | must-have | MVP | wysoka |
| T-BE-004 | Time tracking module: clock-in/out, breaks, timesheet | E08 | must-have | MVP | wysoka |
| T-BE-005 | QR token flow (generate, verify) | E08 | must-have | MVP | średnia |
| T-BE-006 | Schedule CRUD + templates | E08 | must-have | MVP | średnia |
| T-BE-007 | Anomaly detection engine | E08 | must-have | MVP | wysoka |
| T-BE-008 | Workflow engine (state machine, JSON-driven) | E09 | must-have | MVP | wysoka |
| T-BE-009 | Approval flow (single-level) | E09 | must-have | MVP | średnia |
| T-BE-010 | Leave module: requests, balance, calendar | E10 | must-have | MVP | wysoka |
| T-BE-011 | Tasks module: CRUD, statuses, comments, attachments | E11 | must-have | MVP | średnia |
| T-BE-012 | Notification dispatcher (event-driven) | E12 | must-have | MVP | średnia |
| T-BE-013 | Email transport (SMTP adapter) | E12 | must-have | MVP | niska |
| T-BE-014 | SignalR hub (real-time notifications) | E12 | should-have | MVP | średnia |
| T-BE-015 | Document upload/download (MinIO) | E13 | must-have | MVP | niska |
| T-BE-016 | Audit trail (append-only) + API | E13 | must-have | MVP | średnia |
| T-BE-017 | Dashboard aggregation queries (Dapper) | E14 | must-have | MVP | średnia |
| T-BE-018 | Workspace API (/workspace/my-day) | E15 | must-have | MVP | niska |
| T-BE-019 | CSV export (reports) | E14 | should-have | MVP | niska |
| T-BE-020 | Time correction API | E08 | must-have | MVP | niska |

---

# 12. Taski frontend web (agregacja)

| ID | Nazwa | Epik | Priorytet | Etap | Główne widoki | Role |
|---|---|---|---|---|---|---|
| T-FE-001 | Project setup (Vite, React, TS, Router) | E19 | must-have | MVP | — | — |
| T-FE-002 | Design system (Shadcn/ui, Tailwind, theme) | E19 | must-have | MVP | ui/* components | — |
| T-FE-003 | Auth flow (OIDC, token, protected routes) | E19+E02 | must-have | MVP | AuthLayout, login redirect | Wszyscy |
| T-FE-004 | MainLayout (sidebar, topbar, responsive) | E19 | must-have | MVP | MainLayout | Wszyscy |
| T-FE-005 | API client + OpenAPI codegen | E19 | must-have | MVP | — | — |
| T-FE-006 | FeatureGate + PermissionGate components | E19 | must-have | MVP | — | — |
| T-FE-007 | Org tree view + employee list | E05 | must-have | MVP | OrgTreePage, EmployeeListPage | Admin, HR, Kierownik |
| T-FE-008 | Employee form (create/edit) | E05 | must-have | MVP | EmployeeForm (modal) | Admin, HR |
| T-FE-009 | Roles & permissions admin panel | E06 | must-have | MVP | RolesPage, PermissionsMatrixPage | Admin |
| T-FE-010 | Clock-in/out button + timer | E08 | must-have | MVP | ClockButton (shared component) | Pracownik |
| T-FE-011 | Timesheet (day/week/month) | E08 | must-have | MVP | TimesheetPage | Pracownik, Kierownik |
| T-FE-012 | Team attendance report | E08 | must-have | MVP | TeamAttendancePage | Kierownik, HR |
| T-FE-013 | Schedule management (grafik) | E08 | must-have | MVP | SchedulePage | Kierownik, HR |
| T-FE-014 | Leave request form | E10 | must-have | MVP | LeaveRequestPage | Pracownik |
| T-FE-015 | Leave calendar | E10 | must-have | MVP | LeaveCalendarPage | Kierownik, HR |
| T-FE-016 | Leave balance card | E10 | must-have | MVP | LeaveBalancePage | Pracownik |
| T-FE-017 | Pending approvals + action bar | E09 | must-have | MVP | PendingApprovalsPage | Kierownik |
| T-FE-018 | Task list + task card | E11 | must-have | MVP | TaskListPage, TaskCardPage | Pracownik, Kierownik |
| T-FE-019 | My tasks view | E11 | must-have | MVP | MyTasksPage | Pracownik |
| T-FE-020 | Dashboard (management view) | E14 | must-have | MVP | DashboardPage | Kierownik, Dyrektor |
| T-FE-021 | Workspace (my day) | E15 | must-have | MVP | WorkspacePage | Wszyscy |
| T-FE-022 | Employee card 360 | E16 | must-have | MVP | EmployeeCardPage | Kierownik, HR |
| T-FE-023 | Team card | E16 | must-have | MVP | TeamCardPage | Kierownik |
| T-FE-024 | Notifications (bell, list, mark read) | E12 | must-have | MVP | NotificationDropdown | Wszyscy |
| T-FE-025 | Admin: feature flags panel | E03 | must-have | MVP | FeatureFlagsPage | Admin |
| T-FE-026 | Admin: leave types config | E10 | must-have | MVP | LeaveTypesConfigPage | Admin |
| T-FE-027 | Admin: task statuses config | E11 | must-have | MVP | TaskStatusConfigPage | Admin |
| T-FE-028 | CSV import (employees) | E05 | should-have | MVP | ImportCsvPage | Admin, HR |
| T-FE-029 | CSV export (reports) | E14 | should-have | MVP | ExportButton (on reports) | Kierownik |
| T-FE-030 | i18n setup (PL) | E19 | should-have | MVP | — | — |

---

# 13. Taski mobile

| ID | Nazwa | Priorytet | Etap | Złożoność | Uwagi |
|---|---|---|---|---|---|
| T-MOB-001 | PWA manifest + service worker | must-have | MVP | niska | Installable web app on phone |
| T-MOB-002 | Capacitor project setup (Android + iOS config) | should-have | MVP | średnia | Potrzebne tylko jeśli QR scan wymaga natywnego dostępu |
| T-MOB-003 | Mobile layout (bottom tabs: Mój dzień / Zadania / Wnioski / Więcej) | must-have | MVP | średnia | Responsive breakpoint lub osobny layout |
| T-MOB-004 | Clock-in/out (mobile button) | must-have | MVP | niska | Współdzielony ClockButton |
| T-MOB-005 | QR scanner (camera → decode → verify) | must-have | MVP | średnia | Web Camera API lub Capacitor Camera plugin |
| T-MOB-006 | Leave request form (mobile) | should-have | MVP | niska | Responsywny formularz web |
| T-MOB-007 | Approval quick actions (approve/reject) | should-have | MVP | niska | Swipe action lub button |
| T-MOB-008 | My Day overview (uproszczony workspace) | should-have | MVP | niska | Subset danych z /workspace/my-day |
| T-MOB-009 | Push notifications (FCM) | should-have | MVP | średnia | Capacitor Push plugin + backend FCM adapter |

---

# 14. Taski bazy danych

| ID | Nazwa | Epik | Priorytet | Etap | Uwagi |
|---|---|---|---|---|---|
| T-DB-001 | Initial migration: org_ tables | E05 | must-have | MVP | Closure table, indeksy |
| T-DB-002 | Migration: iam_ tables | E06 | must-have | MVP | Seed: Super Admin, Admin, permissions |
| T-DB-003 | Migration: time_ tables | E08 | must-have | MVP | Indeksy na (tenant_id, employee_id, date) |
| T-DB-004 | Migration: wf_ tables | E09 | must-have | MVP | JSONB column dla definition |
| T-DB-005 | Migration: leave_ tables | E10 | must-have | MVP | Seed: domyślne typy nieobecności |
| T-DB-006 | Migration: task_ tables | E11 | must-have | MVP | Seed: domyślne statusy, priorytety |
| T-DB-007 | Migration: notif_ tables | E12 | must-have | MVP | — |
| T-DB-008 | Migration: doc_ + audit_ tables | E13 | must-have | MVP | REVOKE UPDATE/DELETE on audit_entries |
| T-DB-009 | Migration: dash_ config tables | E14 | should-have | MVP | — |
| T-DB-010 | cfg_tenant_configs table | E04 | must-have | MVP | Konfiguracja per tenant per moduł |
| T-DB-011 | cfg_custom_field_definitions table | E16 | should-have | MVP | JSONB custom fields infrastructure |
| T-DB-012 | Seed script: demo tenant + admin + sample data | E01 | must-have | MVP | Developer convenience |
| T-DB-013 | GIN index na JSONB custom_fields | E16 | should-have | MVP | Performance custom field queries |

---

# 15. Taski auth/security

| ID | Nazwa | Priorytet | Etap | Złożoność | Ryzyko |
|---|---|---|---|---|---|
| T-SEC-001 | Keycloak realm + client config (IaC) | must-have | MVP | średnia | średnie |
| T-SEC-002 | JWT bearer validation (.NET) | must-have | MVP | średnia | niskie |
| T-SEC-003 | Custom claim mapper (tenant_id, employee_id) | must-have | MVP | średnia | średnie |
| T-SEC-004 | OIDC PKCE flow (frontend) | must-have | MVP | średnia | średnie |
| T-SEC-005 | RBAC permission middleware | must-have | MVP | wysoka | wysokie |
| T-SEC-006 | Data scope middleware + query filter | must-have | MVP | wysoka | wysokie |
| T-SEC-007 | Feature flag middleware | must-have | MVP | niska | niskie |
| T-SEC-008 | Audit entries immutability (DB-level REVOKE) | must-have | MVP | niska | niskie |
| T-SEC-009 | Rate limiting middleware (per-tenant) | should-have | MVP | niska | niskie |
| T-SEC-010 | CORS configuration (allowed origins) | must-have | MVP | niska | niskie |
| T-SEC-011 | Integration test: permission enforcement | must-have | MVP | średnia | wysokie |
| T-SEC-012 | Integration test: tenant isolation (data leak check) | must-have | MVP | średnia | krytyczne |

---

# 16. Taski integracyjne (post-MVP)

| ID | Nazwa | Priorytet | Etap | Złożoność | Zależności |
|---|---|---|---|---|---|
| T-INT-001 | Integration module scaffold (adapter pattern) | should-have | post-MVP | średnia | E01 |
| T-INT-002 | OAuth2 token store (encrypted) | should-have | post-MVP | średnia | E01 |
| T-INT-003 | Google Calendar sync (push leave → calendar event) | should-have | post-MVP | wysoka | E10 |
| T-INT-004 | Microsoft 365 Calendar sync | should-have | post-MVP | wysoka | E10 |
| T-INT-005 | Slack webhook notifications | nice-to-have | post-MVP | niska | E12 |
| T-INT-006 | Gmail sidebar plugin (link emails to records) | nice-to-have | post-MVP | wysoka | E13 |
| T-INT-007 | Teams bot (notifications) | nice-to-have | post-MVP | średnia | E12 |

---

# 17. Taski devops

| ID | Nazwa | Priorytet | Etap | Złożoność | Uwagi |
|---|---|---|---|---|---|
| T-DEV-001 | Git repo init + branching strategy | must-have | MVP | niska | — |
| T-DEV-002 | Dockerfile (multi-stage: build + runtime) | must-have | MVP | niska | — |
| T-DEV-003 | docker-compose.dev.yml (full stack local) | must-have | MVP | średnia | PG, Keycloak, MinIO, Seq |
| T-DEV-004 | CI pipeline: build + test (.NET) | must-have | MVP | średnia | GitHub Actions |
| T-DEV-005 | CI pipeline: lint + typecheck + build (frontend) | must-have | MVP | niska | GitHub Actions |
| T-DEV-006 | CD pipeline: deploy to staging | should-have | MVP | średnia | Docker push + deploy script |
| T-DEV-007 | Keycloak realm config in repo (IaC) | must-have | MVP | niska | JSON export/import |
| T-DEV-008 | .env.example + secrets documentation | must-have | MVP | niska | — |
| T-DEV-009 | Staging environment setup | should-have | MVP | średnia | VPS / cloud VM |
| T-DEV-010 | Migration runner in CI/CD | should-have | MVP | niska | EF Core migrations |
| T-DEV-011 | Database backup script | should-have | MVP | niska | pg_dump cron |

---

# 18. Taski future-ready pod desktop/kiosk

| ID | Nazwa | Priorytet | Etap | Złożoność | Uwagi |
|---|---|---|---|---|---|
| T-FUT-001 | shared/ folder: zero browser-specific API usage | must-have | MVP | niska | Abstrakcja storage, routing |
| T-FUT-002 | JWT bearer token (nie cookies) | must-have | MVP | niska | Kompatybilne z Capacitor/Tauri |
| T-FUT-003 | Hash routing support (React Router) | nice-to-have | post-MVP | niska | Tauri wymaga hash routing |
| T-FUT-004 | KioskLayout (fullscreen, no nav, large touch targets) | should-have | MVP | średnia | Web fullscreen mode |
| T-FUT-005 | Kiosk system account (per lokalizacja) | should-have | MVP | niska | Osobny Keycloak user „kiosk-{location}" |
| T-FUT-006 | Kiosk auto-refresh / heartbeat | should-have | MVP | niska | Prevent stale state |
| T-FUT-007 | Tauri wrapper POC | nice-to-have | premium | średnia | Proof of concept |
| T-FUT-008 | System tray integration (Tauri) | nice-to-have | premium | średnia | Clock-in/out z tray |

---

# 19. Taski odłożone poza MVP

## Post-MVP

| ID | Nazwa | Typ | Złożoność | Zależności MVP |
|---|---|---|---|---|
| T-POST-001 | Case management module (CRUD, statuses, SLA) | backend + frontend | wysoka | E09 (workflow) |
| T-POST-002 | Contacts module (CRUD kontrahentów, opiekun) | backend + frontend | średnia | E05 (org) |
| T-POST-003 | Google Workspace integration | integracje | wysoka | E10 (leave) |
| T-POST-004 | Microsoft 365 integration | integracje | wysoka | E10 (leave) |
| T-POST-005 | Slack integration | integracje | niska | E12 (notifications) |
| T-POST-006 | Advanced workflow engine (conditions, branching, multi-level) | backend | wysoka | E09 (engine) |
| T-POST-007 | Visual workflow builder (frontend) | frontend | wysoka | T-POST-006 |
| T-POST-008 | Form builder (custom form definitions) | backend + frontend | wysoka | T-POST-006 |
| T-POST-009 | Advanced reporting (trends, charts, scheduled) | backend + frontend | wysoka | E14 (dashboard) |
| T-POST-010 | Configurable dashboards (drag & drop widgets) | frontend | średnia | E14 |
| T-POST-011 | Public API (subset, key management, rate limiting, docs) | backend | średnia | E01 |
| T-POST-012 | Webhooks (events → external endpoints) | backend | średnia | E04 (events) |
| T-POST-013 | NFC clock-in (Capacitor native plugin) | mobile | średnia | E08, E17 |
| T-POST-014 | Push notifications (FCM full pipeline) | mobile + backend | średnia | E12, E17 |
| T-POST-015 | Multiple roles per user | backend + frontend | średnia | E06 |
| T-POST-016 | Custom card section configurations per role | frontend | średnia | E16 |
| T-POST-017 | Saved views (filters + columns + sort) | frontend | średnia | — |
| T-POST-018 | Activity feed (global stream) | backend + frontend | średnia | E13 (audit) |

## Premium / Future

| ID | Nazwa | Typ | Złożoność | Zależności |
|---|---|---|---|---|
| T-PREM-001 | Sales module (leads, pipeline, opportunities, offers) | backend + frontend | wysoka | T-POST-002 |
| T-PREM-002 | AI summarization (employee, case, history) | AI + backend | wysoka | Dane historyczne |
| T-PREM-003 | AI classification (zgłoszenia) | AI + backend | średnia | T-POST-001 |
| T-PREM-004 | AI next-step suggestion | AI + backend | średnia | E11 (tasks) |
| T-PREM-005 | AI semantic search (documents + records) | AI + backend | wysoka | E13 (docs) |
| T-PREM-006 | Department modules (IT, HR, logistics) | backend + frontend | wysoka | T-POST-008 (form builder) |
| T-PREM-007 | Tauri desktop wrapper | desktop | średnia | Stabilny web app |
| T-PREM-008 | Biometry (face, fingerprint) | mobile/hardware | wysoka | E08 (time) |
| T-PREM-009 | Geofencing (auto clock-in) | mobile + backend | średnia | E08 (time) |
| T-PREM-010 | White-label (custom branding per tenant) | frontend + backend | średnia | — |
| T-PREM-011 | Self-service tenant onboarding | frontend + backend | średnia | — |
| T-PREM-012 | Billing integration (Stripe / subscription plans) | backend + frontend | wysoka | — |
| T-PREM-013 | Offline mode (mobile sync engine) | mobile | wysoka | E17 |
| T-PREM-014 | Schema per tenant (DB isolation) | architektura | wysoka | E03 |

---

## Oznaczenia konfigurowalności i opcjonalności

### Elementy KONFIGUROWALNE (mechanizm, nie twarda implementacja)

| Element | Gdzie konfigurowany | Wpływa na |
|---|---|---|
| Typy jednostek organizacyjnych | Admin panel → cfg per tenant | Drzewo org, nazewnictwo |
| Role i uprawnienia | Admin panel → matryca ról | Dostęp, widoczność, akcje |
| Data scope per rola | Admin panel → scope config | Filtrowanie danych |
| Typy nieobecności | Admin panel → leave_types | Formularz urlopowy, saldo |
| Limity urlopowe | Admin panel → leave_policies | Naliczanie, walidacja |
| Statusy zadań + przejścia | Admin panel → task_statuses | Flow pracy |
| Priorytety zadań | Admin panel → task_priorities | Priorytetyzacja |
| Workflow definitions (JSON) | Admin panel → wf_definitions | Approval flow, automatyzacje |
| Progi anomalii czasu pracy | Admin panel → cfg_tenant_configs | Alerty, reguły |
| Schedule templates (zmiany) | Admin panel → schedule_templates | Grafik, anomalie |
| Feature flags (moduły) | Admin panel → iam_feature_flags | Włączone/wyłączone moduły |
| Custom fields per entity | Admin panel → cfg_custom_field_definitions | Karty 360, formularze |
| Notification templates | Admin panel → notif_templates | Treść powiadomień |

### Elementy OPCJONALNE (moduły włączane per tenant)

| Moduł | Feature flag | Etap |
|---|---|---|
| Urlopy i nieobecności | `module.leave` | MVP (domyślnie włączony) |
| Case management | `module.cases` | post-MVP |
| Kontrahenci | `module.contacts` | post-MVP |
| Sprzedaż | `module.sales` | premium |
| Integracja Google | `module.integration.google` | post-MVP |
| Integracja Microsoft | `module.integration.microsoft` | post-MVP |
| Integracja Slack | `module.integration.slack` | post-MVP |
| AI | `module.ai` | premium |
| Moduły działowe | `module.departments.*` | premium |

---

> **Status:** Backlog gotowy do przeniesienia do narzędzia (Jira / Linear / Azure Boards). Każdy T-* to task, każdy T-*-a/b/c to subtask. Priorytety i fazy ustalone.
