# WorkBase — Architektura Techniczna

> Dokument roboczy dla zespołu technicznego.
> Wersja: 0.1 | Data: 2026-03-24 | Status: Draft
> Dokumenty bazowe: 01-product-foundation.md, 02-mvp-roadmap.md

---

# 1. Założenia architektoniczne

## 1.1. Stack technologiczny

| Warstwa | Technologia | Wersja | Uzasadnienie |
|---|---|---|---|
| Backend | ASP.NET Core + C# | .NET 9 | Wydajność, typowanie, ekosystem enterprise, modularność |
| Frontend | React + TypeScript | React 19 | Komponentowy, duży ekosystem, współdzielenie z mobile/desktop |
| Baza danych | PostgreSQL | 16+ | JSONB (custom fields), wydajność, darmowy, dojrzały |
| ORM | Entity Framework Core | 9.x | Migracje, LINQ, integracja z .NET |
| Read-heavy queries | Dapper | opcjonalnie | Raporty, dashboardy — raw SQL dla wydajności |
| Auth / IAM | Keycloak | 24+ | SSO, OIDC, RBAC, multi-tenant, 2FA, darmowy |
| Logi | Serilog | latest | Structured logging, sinki (Seq, Elasticsearch, Console) |
| Background jobs | Hangfire | latest | Cron, delayed, fire-and-forget, dashboard, persistent |
| Real-time | SignalR | built-in .NET | WebSocket z fallbackiem, powiadomienia live |
| Object storage | MinIO (self-hosted) / S3 (cloud) | — | Załączniki, dokumenty |
| Konteneryzacja | Docker + docker-compose | — | Dev env, staging, produkcja |
| CI/CD | GitHub Actions | — | Build, test, deploy, migrations |
| Reverse proxy | Nginx / Traefik | — | SSL termination, routing, rate limiting |

## 1.2. Zasady naczelne

1. **Modular monolith** — nie mikroserwisy. Jeden deployment, wydzielone moduły.
2. **API-first** — każdy klient (web, mobile, kiosk, desktop) komunikuje się przez to samo REST API.
3. **One database** — jedna instancja PostgreSQL, logiczny podział per moduł.
4. **Multi-tenant by default** — `tenant_id` na każdej tabeli, izolacja na poziomie query.
5. **Configuration over code** — role, statusy, workflow, pola — w bazie, nie w kodzie źródłowym.
6. **Append-only audit** — niemodyfikowalny log zdarzeń.
7. **Feature flags per tenant** — moduły opcjonalne włączane/wyłączane.
8. **Web-first, API-first, mobile-ready** — UI projektowane pod przeglądarkę, ale API gotowe na mobile.

## 1.3. Wzorce architektoniczne

| Wzorzec | Zastosowanie |
|---|---|
| Clean Architecture (light) | Podział warstw w każdym module: Domain → Application → Infrastructure → API |
| CQRS (light) | Oddzielne modele read/write tam, gdzie potrzebne (dashboardy, raporty) — nie event sourcing |
| Mediator (MediatR) | Dispatching command/query w Application layer |
| Repository pattern | Abstrakcja dostępu do danych w Infrastructure |
| Domain Events | Komunikacja między modułami — event publikowany, inny moduł reaguje |
| Specification pattern | Złożone filtry i query (listy, wyszukiwanie) |
| Strategy pattern | Workflow engine — reguły przejść, walidacje, akcje automatyczne |

---

# 2. Dlaczego modular monolith

## 2.1. Argumenty za

| Argument | Opis |
|---|---|
| Prostota deploymentu | Jeden artefakt, jedno CI/CD pipeline, jeden serwer |
| Prostota debugowania | Stack trace end-to-end, brak network hops między serwisami |
| Transakcje | Spójna transakcja bazodanowa między modułami (np. utwórz wniosek + przypisz akceptanta) |
| Szybkość rozwoju MVP | Brak narzutu infra mikroserwisów (service discovery, API gateway, distributed tracing) |
| Refaktor jest łatwiejszy | Przesuwanie kodu między modułami to refaktor, nie redesign |
| Wystarczająco skalowalny | Dla zakładanego obciążenia MVP (~50 tenantów, ~5000 users) monolith jest OK |

## 2.2. Jak zapewniamy modularność mimo monolitu

| Mechanizm | Opis |
|---|---|
| Osobne projekty .NET per moduł | Moduł = osobny .csproj dla Domain, Application, Infrastructure |
| Brak bezpośrednich referencji między modułami | Moduł A nie importuje typów modułu B |
| Komunikacja przez Domain Events | Moduł publikuje zdarzenie → Shared Event Bus → inny moduł reaguje |
| Shared Kernel | Wspólne typy bazowe (Entity, AuditableEntity, ValueObject, Result) w osobnym projekcie |
| Integration Contracts | Moduły wystawiają kontrakty (interfejsy, DTO) w osobnym projekcie Contracts |
| Osobne migracje | Każdy moduł ma własne migracje EF Core (opcjonalnie: osobne DbContexty) |

## 2.3. Ścieżka do mikroserwisów (jeśli kiedyś będzie potrzebna)

Jeśli system urośnie poza wydajność monolitu:
1. Wydzielamy moduł jako osobny serwis (ma już własną domenę, API, migracje)
2. Zamieniamy in-process domain events na message broker (RabbitMQ / Kafka)
3. Wydzielamy bazę (osobny schema lub osobna instancja)

> **Nie planujemy tego na MVP ani post-MVP.** Ale architektura modularnego monolitu to umożliwia bez przepisywania.

---

# 3. Moduły domenowe backendu

## 3.1. Mapa modułów

```
┌──────────────────────────────────────────────────────────────┐
│                        API Gateway / Host                     │
│           (ASP.NET Core host, routing, middleware)            │
├──────────────────────────────────────────────────────────────┤
│                      Shared Kernel                            │
│  (Entity, ValueObject, Result, DomainEvent, AuditableEntity) │
├──────────┬──────────┬──────────┬──────────┬─────────────────┤
│  Org     │ Identity │ Time     │ Leave    │ Tasks           │
│  Module  │ Module   │ Module   │ Module   │ Module          │
├──────────┼──────────┼──────────┼──────────┼─────────────────┤
│ Workflow │ Mgmt     │ Notif    │ Docs     │ Audit           │
│ Module   │ Dashboard│ Module   │ Module   │ Module          │
├──────────┼──────────┼──────────┼──────────┼─────────────────┤
│ Cases    │ Contacts │ Sales    │ AI       │ Integrations    │
│ Module   │ Module   │ Module   │ Module   │ Module          │
│ (post)   │ (post)   │ (prem)   │ (prem)   │ (post)          │
└──────────┴──────────┴──────────┴──────────┴─────────────────┘
```

## 3.2. Moduły rdzeniowe (MVP)

### MOD-ORG: Organization Module
**Odpowiedzialność:** Struktura organizacyjna, jednostki, hierarchia, stanowiska

**Encje domenowe:**
- `Tenant` — firma/organizacja (tenant)
- `OrganizationUnit` — jednostka organizacyjna (firma, oddział, dział, zespół…)
- `OrganizationUnitType` — typ jednostki (konfigurowalny per tenant)
- `OrganizationUnitClosure` — closure table dla hierarchii
- `Position` — stanowisko (np. „Specjalista ds. marketingu")
- `Employee` — pracownik (profil biznesowy, powiązany z Identity)
- `EmployeeAssignment` — przypisanie pracowka do jednostki + stanowiska
- `SupervisorRelation` — relacja przełożony–podwładny

**Główne use case'y / API:**
- `POST /api/org/units` — utwórz jednostkę organizacyjną
- `GET /api/org/units/tree` — drzewo organizacyjne (hierarchia)
- `PUT /api/org/units/{id}` — edytuj jednostkę
- `POST /api/org/employees` — utwórz/dodaj pracownika
- `GET /api/org/employees` — lista pracowników (z filtrowaniem, scope)
- `GET /api/org/employees/{id}` — karta pracownika (dane + powiązania)
- `PUT /api/org/employees/{id}/assignment` — przypisz do jednostki/stanowiska
- `PUT /api/org/employees/{id}/supervisor` — ustaw przełożonego
- `POST /api/org/employees/import` — import CSV
- `GET /api/org/positions` — lista stanowisk
- `GET /api/org/unit-types` — lista typów jednostek (konfiguracja)

**Mechanizmy (nie twarda implementacja):**
- Closure table — generyczny, nie zakładający konkretnych poziomów
- Typ jednostki — konfigurowalny per tenant (admin definiuje nazwy i głębokość)
- Stanowisko — słownik per tenant

**Eventy domenowe:**
- `EmployeeCreated`, `EmployeeDeactivated`, `EmployeeAssignmentChanged`, `SupervisorChanged`

---

### MOD-IDENTITY: Identity & Access Module
**Odpowiedzialność:** Użytkownicy, role, uprawnienia, sesje, integracja z Keycloak

**Encje domenowe:**
- `User` — konto użytkownika (login, email, status, tenant_id)
- `Role` — rola (nazwa, opis, typ: systemowa/organizacyjna, tenant_id)
- `Permission` — uprawnienie (moduł + akcja + scope)
- `RolePermission` — przypisanie uprawnień do roli
- `UserRole` — przypisanie roli do użytkownika
- `DataScope` — zakres widoczności (reguły per rola per moduł)
- `FeatureFlag` — flaga modułu per tenant (włączony/wyłączony)

**Główne use case'y / API:**
- `POST /api/auth/login` — logowanie (redirect do Keycloak lub token exchange)
- `POST /api/auth/refresh` — odświeżenie tokena
- `GET /api/auth/me` — moje dane + rola + uprawnienia + scope
- `GET /api/identity/roles` — lista ról
- `POST /api/identity/roles` — utwórz rolę
- `PUT /api/identity/roles/{id}/permissions` — matryca uprawnień
- `GET /api/identity/users` — lista użytkowników (admin)
- `PUT /api/identity/users/{id}/roles` — przypisz rolę
- `GET /api/identity/features` — lista modułów z flagami (admin)
- `PUT /api/identity/features/{moduleId}` — włącz/wyłącz moduł
- `GET /api/identity/scopes` — reguły widoczności

**Mechanizmy:**
- RBAC z atrybutami kontekstowymi (rola + pozycja w strukturze + moduł)
- Data scope engine — middleware filtrujące query wg scope użytkownika
- Feature flag registry — sprawdzanie czy moduł jest aktywny per tenant
- Synchronizacja z Keycloak (role mapping, user provisioning)

**Eventy domenowe:**
- `UserCreated`, `UserDeactivated`, `RoleChanged`, `PermissionsUpdated`, `FeatureFlagToggled`

---

### MOD-TIME: Time Tracking Module
**Odpowiedzialność:** Rejestracja czasu pracy, przerwy, timesheet, grafik, anomalie

**Encje domenowe:**
- `TimeEntry` — wpis czasu pracy (clock-in, clock-out, typ: work/break/in/out)
- `TimeSheet` — agregat dzienny (data, pracownik, suma godzin, status)
- `Schedule` — grafik/zmiana (pracownik, dzień, planowane godziny od–do)
- `ScheduleTemplate` — szablon zmianowy (konfigurowalny)
- `TimeAnomaly` — wykryta anomalia (typ, status: new/reviewed/dismissed, link do TimeSheet)
- `TimeCorrection` — korekta czasu pracy (przez kierownika, z uzasadnieniem)
- `ClockMethod` — metoda rejestracji (manual, qr, nfc, kiosk, geolocation)
- `QrToken` — tymczasowy token QR do clock-in

**Główne use case'y / API:**
- `POST /api/time/clock-in` — rozpocznij pracę (z metadanymi: method, location)
- `POST /api/time/clock-out` — zakończ pracę
- `POST /api/time/break/start` — rozpocznij przerwę
- `POST /api/time/break/end` — zakończ przerwę
- `GET /api/time/today` — mój dzisiejszy status (clock-in, godziny, status)
- `GET /api/time/timesheet?from=&to=` — mój timesheet za okres
- `GET /api/time/team/{unitId}/today` — obecność zespołu (kierownik)
- `GET /api/time/team/{unitId}/report?from=&to=` — raport czasu pracy zespołu
- `GET /api/time/anomalies` — lista anomalii (kierownik)
- `POST /api/time/corrections` — korekta czasu pracy (kierownik)
- `POST /api/time/qr/generate` — generuj QR do kiosku
- `POST /api/time/qr/verify` — zweryfikuj skan QR
- `GET /api/time/schedules` — grafiki pracowników
- `POST /api/time/schedules` — ustaw grafik

**Mechanizmy:**
- Anomaly detection engine — zestaw reguł konfigurowalnych per tenant (progi, typy)
- Schedule matcher — porównanie TimeEntry z Schedule → wykrywanie spóźnień, nadgodzin
- QR token generator — jednorazowy/czasowy token z TTL
- Clock method validator — walidacja metody (np. kiosk wymaga PIN/QR)

**Eventy domenowe:**
- `ClockInRecorded`, `ClockOutRecorded`, `BreakStarted`, `BreakEnded`, `AnomalyDetected`, `TimeCorrected`

---

### MOD-LEAVE: Leave & Absence Module
**Odpowiedzialność:** Urlopy, nieobecności, wnioski, limity, kalendarz

**Encje domenowe:**
- `LeaveType` — typ nieobecności (konfigurowalna lista per tenant)
- `LeavePolicy` — polityka urlopowa (ile dni, naliczanie, przenoszenie)
- `LeaveBalance` — saldo per pracownik per typ per rok
- `LeaveRequest` — wniosek urlopowy (daty, typ, status, komentarz)
- `LeaveDecision` — decyzja akceptanta (approve/reject/return, komentarz, timestamp)
- `LeaveCalendarEntry` — wpis kalendarza nieobecności (widok)
- `LeaveConflict` — wykryty konflikt (za dużo osób na urlopie)

**Główne use case'y / API:**
- `GET /api/leave/types` — typy nieobecności (konfiguracja)
- `GET /api/leave/balance` — moje saldo urlopów
- `GET /api/leave/balance/team/{unitId}` — salda zespołu (kierownik)
- `POST /api/leave/requests` — złóż wniosek
- `GET /api/leave/requests` — moje wnioski (historia)
- `GET /api/leave/requests/pending` — wnioski oczekujące na moją akceptację
- `POST /api/leave/requests/{id}/approve` — zatwierdź
- `POST /api/leave/requests/{id}/reject` — odrzuć
- `POST /api/leave/requests/{id}/return` — cofnij do poprawy
- `GET /api/leave/calendar?unitId=&from=&to=` — kalendarz nieobecności
- `GET /api/leave/conflicts?unitId=&from=&to=` — konflikty urlopowe

**Mechanizmy:**
- Leave balance calculator — naliczanie proporcjonalne, przenoszenie
- Conflict detector — konfigurowalny próg jednoczesnych nieobecności per zespół
- Zależy od MOD-WORKFLOW (approval flow)

**Eventy domenowe:**
- `LeaveRequested`, `LeaveApproved`, `LeaveRejected`, `LeaveBalanceUpdated`, `LeaveConflictDetected`

---

### MOD-TASKS: Task Management Module
**Odpowiedzialność:** Zadania, delegowanie, statusy, priorytety, follow-up

**Encje domenowe:**
- `Task` — zadanie (tytuł, opis, deadline, priorytet, status, assignee, reporter)
- `TaskStatus` — konfigurowalna lista statusów per tenant
- `TaskPriority` — konfigurowalna lista priorytetów per tenant
- `TaskStatusTransition` — dozwolone przejścia między statusami
- `TaskComment` — komentarz do zadania
- `TaskAttachment` — załącznik do zadania
- `TaskHistory` — historia zmian (kto, co, kiedy)
- `TaskReminder` — przypomnienie (scheduled)
- `TaskDelegation` — log delegowania/przekazywania

**Główne use case'y / API:**
- `POST /api/tasks` — utwórz zadanie
- `GET /api/tasks` — lista zadań (z filtrowaniem, paginacją, scope)
- `GET /api/tasks/my` — moje zadania
- `GET /api/tasks/{id}` — szczegóły zadania (karta 360)
- `PUT /api/tasks/{id}` — edytuj zadanie
- `PUT /api/tasks/{id}/status` — zmień status (z walidacją przejścia)
- `PUT /api/tasks/{id}/assign` — przypisz/deleguj
- `POST /api/tasks/{id}/comments` — dodaj komentarz
- `POST /api/tasks/{id}/attachments` — dodaj załącznik
- `GET /api/tasks/{id}/history` — historia zmian
- `GET /api/tasks/overdue` — zaległe zadania (kierownik)
- `GET /api/tasks/team/{unitId}/summary` — podsumowanie zadań zespołu

**Mechanizmy:**
- Status machine — konfigurowalny per tenant (statusy + przejścia)
- Priority engine — konfigurowalne priorytety per tenant
- Overdue detector — background job sprawdzający deadline'y → alerty
- Reminder scheduler — Hangfire jobs dla przypomnień

**Eventy domenowe:**
- `TaskCreated`, `TaskStatusChanged`, `TaskAssigned`, `TaskDelegated`, `TaskOverdue`, `TaskCommented`

---

### MOD-WORKFLOW: Workflow & Approval Engine
**Odpowiedzialność:** Generyczny silnik procesów, akceptacje, automatyzacje

**Encje domenowe:**
- `WorkflowDefinition` — definicja workflow (JSON, wersjonowana, per tenant)
- `WorkflowInstance` — instancja uruchomionego procesu
- `WorkflowStep` — etap w instancji (status, akceptant, timestamp)
- `WorkflowTransition` — przejście między etapami (reguły)
- `ApprovalRequest` — żądanie akceptacji (powiązane z step)
- `ApprovalDecision` — decyzja (approve/reject/return)
- `WorkflowAction` — automatyczna akcja (wyślij powiadomienie, utwórz zadanie, eskaluj)
- `EscalationRule` — reguła eskalacji (timeout → akcja)

**Główne use case'y / API:**
- `GET /api/workflow/definitions` — lista definicji workflow (admin)
- `POST /api/workflow/definitions` — utwórz definicję
- `PUT /api/workflow/definitions/{id}` — edytuj definicję
- `POST /api/workflow/instances` — uruchom workflow (wewnętrznie przy tworzeniu wniosku)
- `GET /api/workflow/instances/{id}` — status instancji
- `GET /api/workflow/approvals/pending` — moje oczekujące akceptacje
- `POST /api/workflow/approvals/{id}/approve` — zatwierdź
- `POST /api/workflow/approvals/{id}/reject` — odrzuć
- `POST /api/workflow/approvals/{id}/return` — cofnij do poprawy
- `GET /api/workflow/approvals/history` — historia moich decyzji

**Mechanizmy (framework, nie twarda implementacja):**
- State machine engine — generyczny, definicja w JSON, brak kodu per typ procesu
- Approval chain resolver — wyznaczanie akceptanta wg reguł (relacja przełożony, rola, konkretna osoba)
- Action executor — wykonywanie automatycznych akcji po przejściu etapu
- Escalation scheduler — Hangfire job monitorujący timeouty
- Workflow Definition Schema — JSON schema definiujący format definicji workflow

```
Przykładowa definicja workflow (JSON):
{
  "id": "leave-request-v1",
  "name": "Wniosek urlopowy",
  "initialStep": "submitted",
  "steps": [
    {
      "id": "submitted",
      "name": "Złożony",
      "type": "approval",
      "approverRule": { "type": "supervisor", "of": "requester" },
      "transitions": [
        { "to": "approved", "action": "approve" },
        { "to": "rejected", "action": "reject" },
        { "to": "returned", "action": "return" }
      ],
      "escalation": { "timeoutHours": 48, "action": "notify_approver_reminder" }
    },
    {
      "id": "approved",
      "name": "Zatwierdzony",
      "type": "final",
      "actions": [
        { "type": "update_leave_balance" },
        { "type": "notify", "target": "requester", "template": "leave_approved" },
        { "type": "create_calendar_entry" }
      ]
    },
    ...
  ]
}
```

**Eventy domenowe:**
- `WorkflowStarted`, `WorkflowStepCompleted`, `ApprovalRequested`, `ApprovalDecisionMade`, `WorkflowCompleted`, `EscalationTriggered`

---

### MOD-DASHBOARD: Management Dashboard Module
**Odpowiedzialność:** Widok kierowniczy, agregaty, alerty, raporty podstawowe

**Encje domenowe:**
- `DashboardConfig` — konfiguracja dashboardu per rola per tenant
- `DashboardWidget` — definicja widżetu (typ, źródło danych, parametry)
- `Alert` — alert operacyjny (typ, severity, powiązany obiekt, status: new/seen/dismissed)
- `ReportDefinition` — definicja raportu (parametry, filtry, format)
- `ReportExport` — wyeksportowany raport (plik, timestamp)

**Główne use case'y / API:**
- `GET /api/dashboard` — mój dashboard (widżety per rola)
- `GET /api/dashboard/attendance/today?unitId=` — obecność dziś
- `GET /api/dashboard/tasks/summary?unitId=` — podsumowanie zadań
- `GET /api/dashboard/approvals/pending` — oczekujące akceptacje
- `GET /api/dashboard/anomalies?unitId=` — anomalie czasu pracy
- `GET /api/dashboard/alerts` — moje alerty
- `PUT /api/dashboard/alerts/{id}/dismiss` — odrzuć alert
- `GET /api/reports/time?unitId=&from=&to=&format=` — raport czasu pracy z eksportem
- `GET /api/reports/leave?unitId=&from=&to=&format=` — raport nieobecności z eksportem

**Mechanizmy:**
- Widget data aggregator — zbiera dane z wielu modułów (Time, Leave, Tasks, Workflow)
- Alert engine — background job generujący alerty na podstawie reguł
- Report generator — generowanie CSV/Excel z widoków
- Dashboard jest **read-only** — nie modyfikuje danych, tylko agreguje

**Uwaga:** Dashboard używa Dapper do bezpośrednich query SQL (read-heavy). Nie przechodzi przez EF Core do odczytu agregacji.

---

### MOD-NOTIFICATION: Notification Module
**Odpowiedzialność:** Powiadomienia in-app, email, push, szablony

**Encje domenowe:**
- `Notification` — powiadomienie (odbiorca, typ, treść, status: unread/read, link)
- `NotificationTemplate` — szablon powiadomienia (per język, per kanał)
- `NotificationPreference` — preferencje użytkownika (co chce dostawać i jak)
- `NotificationChannel` — kanał dostarczenia (in_app, email, push, webhook)

**Główne use case'y / API:**
- `GET /api/notifications` — moje powiadomienia (paginacja)
- `GET /api/notifications/unread/count` — liczba nieprzeczytanych
- `PUT /api/notifications/{id}/read` — oznacz jako przeczytane
- `PUT /api/notifications/read-all` — oznacz wszystkie
- `GET /api/notifications/preferences` — moje preferencje
- `PUT /api/notifications/preferences` — ustaw preferencje

**Mechanizmy:**
- Notification dispatcher — nasłuchuje domain events → generuje powiadomienia
- Email sender — SMTP / SendGrid / Mailgun adapter
- Push sender — FCM adapter (post-MVP)
- SignalR hub — real-time powiadomienia in-app
- Template engine — Scriban/Handlebars do renderowania treści

**Eventy (konsumuje):**
- Nasłuchuje na: `LeaveApproved`, `TaskAssigned`, `AnomalyDetected`, `ApprovalRequested`, `EscalationTriggered` itd.

---

### MOD-DOCS: Document & Audit Module
**Odpowiedzialność:** Załączniki, dokumenty, audyt trail

**Encje domenowe:**
- `Document` — dokument/załącznik (nazwa, typ, rozmiar, storage key, powiązany rekord)
- `DocumentCategory` — kategoria (umowa, certyfikat, zaświadczenie…)
- `DocumentExpiry` — termin ważności (z alertem)
- `AuditEntry` — wpis audytu (append-only: user, action, entity, old/new values, timestamp, tenant)

**Główne use case'y / API:**
- `POST /api/docs/upload` — upload pliku (zwraca document ID)
- `GET /api/docs/{id}/download` — download pliku
- `GET /api/docs?entityType=&entityId=` — dokumenty powiązane z rekordem
- `DELETE /api/docs/{id}` — soft delete dokumentu
- `GET /api/audit?entityType=&entityId=&from=&to=` — log audytu per rekord
- `GET /api/audit/search?userId=&action=&from=&to=` — wyszukiwanie w audycie

**Mechanizmy:**
- File storage adapter — abstrakcja nad MinIO/S3/filesystem
- Audit interceptor — automatyczne przechwytywanie zmian w EF Core (SaveChanges override)
- Audit entry jest **immutable** — brak UPDATE/DELETE na tabeli audytu
- Document expiry checker — Hangfire job alertujący o kończących się dokumentach

**Eventy domenowe:**
- `DocumentUploaded`, `DocumentDeleted`, `DocumentExpiring`

---

## 3.3. Moduły opcjonalne (post-MVP / premium)

### MOD-CASES: Case Management Module (post-MVP)

**Encje:** `Case`, `CaseType`, `CaseStatus`, `CaseComment`, `CaseAttachment`, `CaseHistory`, `CaseSla`

**API:** CRUD spraw, zmiana statusu, przypisanie, komentarze, SLA tracking, eskalacje

**Zależy od:** MOD-WORKFLOW, MOD-NOTIFICATION, MOD-DOCS

---

### MOD-CONTACTS: Contacts & Relations Module (post-MVP)

**Encje:** `Contact` (kontrahent), `ContactPerson`, `ContactRelation` (opiekun), `ContactNote`, `ContactStatus`

**API:** CRUD kontrahentów, przypisanie opiekuna, notatki, historia, powiązane zadania

**Zależy od:** MOD-ORG, MOD-TASKS, MOD-DOCS

---

### MOD-SALES: Sales Module (premium)

**Encje:** `Lead`, `Opportunity`, `Pipeline`, `PipelineStage`, `Offer`, `SalesForecast`

**API:** CRUD leadów, pipeline, szanse, oferty, forecast

**Zależy od:** MOD-CONTACTS, MOD-WORKFLOW, MOD-TASKS

---

### MOD-INTEGRATIONS: External Integrations Module (post-MVP)

**Encje:** `IntegrationConfig` (per tenant per provider), `IntegrationToken` (OAuth tokens), `SyncLog`

**Sub-moduły:** Google Workspace adapter, Microsoft 365 adapter, Slack adapter

**API:** Konfiguracja integracji, OAuth callback, sync triggers

---

### MOD-AI: AI Module (premium)

**Encje:** `AiRequest`, `AiResponse`, `AiPromptTemplate`

**API:** `/api/ai/summarize`, `/api/ai/classify`, `/api/ai/suggest-next-step`, `/api/ai/draft-response`

**Zależy od:** Dane historyczne z MOD-TIME, MOD-TASKS, MOD-CASES

---

# 4. Proponowana struktura solution .NET

```
WorkBase.sln
│
├── src/
│   ├── WorkBase.Host/                          # ASP.NET Core Web Host (startup, DI, middleware, routing)
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Middleware/
│   │   │   ├── TenantResolutionMiddleware.cs
│   │   │   ├── DataScopeMiddleware.cs
│   │   │   ├── FeatureFlagMiddleware.cs
│   │   │   └── ExceptionHandlingMiddleware.cs
│   │   └── Extensions/
│   │       └── ModuleRegistrationExtensions.cs
│   │
│   ├── WorkBase.Shared/                        # Shared Kernel
│   │   ├── Domain/
│   │   │   ├── Entity.cs
│   │   │   ├── AuditableEntity.cs
│   │   │   ├── ValueObject.cs
│   │   │   ├── IDomainEvent.cs
│   │   │   ├── ITenantScoped.cs              # interface { Guid TenantId }
│   │   │   └── Result.cs
│   │   ├── Application/
│   │   │   ├── ICommand.cs / IQuery.cs
│   │   │   ├── PagedResult.cs
│   │   │   └── ICurrentUserService.cs
│   │   └── Infrastructure/
│   │       ├── IAuditInterceptor.cs
│   │       └── IFileStorage.cs
│   │
│   ├── WorkBase.Contracts/                     # Inter-module contracts (DTOs, interfaces)
│   │   ├── Organization/
│   │   │   ├── IOrganizationService.cs
│   │   │   └── EmployeeDto.cs
│   │   ├── Identity/
│   │   │   ├── IPermissionChecker.cs
│   │   │   └── UserContextDto.cs
│   │   ├── Workflow/
│   │   │   ├── IWorkflowEngine.cs
│   │   │   └── ApprovalRequestDto.cs
│   │   ├── Notification/
│   │   │   └── INotificationDispatcher.cs
│   │   └── Time/
│   │       └── AttendanceSummaryDto.cs
│   │
│   ├── Modules/
│   │   ├── WorkBase.Modules.Organization/
│   │   │   ├── Domain/
│   │   │   │   ├── Entities/
│   │   │   │   ├── ValueObjects/
│   │   │   │   ├── Events/
│   │   │   │   └── Specifications/
│   │   │   ├── Application/
│   │   │   │   ├── Commands/
│   │   │   │   ├── Queries/
│   │   │   │   ├── EventHandlers/
│   │   │   │   └── Validators/
│   │   │   ├── Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── OrganizationDbContext.cs
│   │   │   │   │   ├── Configurations/         # EF Core entity configs
│   │   │   │   │   ├── Migrations/
│   │   │   │   │   └── Repositories/
│   │   │   │   └── Services/
│   │   │   └── Api/
│   │   │       ├── Controllers/
│   │   │       ├── Requests/                   # API request DTOs
│   │   │       ├── Responses/                  # API response DTOs
│   │   │       └── OrganizationModule.cs       # Module registration
│   │   │
│   │   ├── WorkBase.Modules.Identity/          # (taka sama struktura)
│   │   ├── WorkBase.Modules.TimeTracking/
│   │   ├── WorkBase.Modules.Leave/
│   │   ├── WorkBase.Modules.Tasks/
│   │   ├── WorkBase.Modules.Workflow/
│   │   ├── WorkBase.Modules.Dashboard/
│   │   ├── WorkBase.Modules.Notification/
│   │   ├── WorkBase.Modules.Documents/
│   │   │
│   │   └── (post-MVP / premium)
│   │       ├── WorkBase.Modules.Cases/
│   │       ├── WorkBase.Modules.Contacts/
│   │       ├── WorkBase.Modules.Sales/
│   │       ├── WorkBase.Modules.Integrations/
│   │       └── WorkBase.Modules.AI/
│   │
│   └── WorkBase.Infrastructure/                # Cross-cutting infrastructure
│       ├── Persistence/
│       │   ├── TenantDbContext.cs              # Shared DB context with tenant filter
│       │   └── AuditSaveChangesInterceptor.cs
│       ├── FileStorage/
│       │   ├── MinioFileStorage.cs
│       │   └── LocalFileStorage.cs
│       ├── Email/
│       │   └── SmtpEmailSender.cs
│       ├── Auth/
│       │   └── KeycloakTokenValidator.cs
│       └── BackgroundJobs/
│           └── HangfireConfiguration.cs
│
├── tests/
│   ├── WorkBase.Tests.Unit/
│   │   ├── Modules.Organization/
│   │   ├── Modules.TimeTracking/
│   │   └── ...
│   ├── WorkBase.Tests.Integration/
│   │   ├── Modules.Organization/
│   │   ├── Modules.TimeTracking/
│   │   └── ...
│   └── WorkBase.Tests.Architecture/            # ArchUnit-style tests
│       └── ModuleBoundaryTests.cs              # Weryfikacja, że moduły nie łamią granic
│
├── docker/
│   ├── docker-compose.yml
│   ├── docker-compose.dev.yml
│   ├── Dockerfile
│   └── keycloak/
│       └── realm-config.json
│
└── docs/
```

### 4.1. Zasady referencji między projektami

```
Host → odwołuje się do WSZYSTKICH modułów (rejestracja)
Module.Api → Module.Application → Module.Domain
Module.Infrastructure → Module.Application, Module.Domain
Module.* → WorkBase.Shared (Shared Kernel)
Module.* → WorkBase.Contracts (Inter-module contracts)
Module.A NIE → Module.B (nigdy bezpośrednia referencja)
```

### 4.2. Architektura modułu (wewnętrzna)

```
┌─────────────────────────────────┐
│            Api Layer            │  Controllers, Request/Response DTOs
├─────────────────────────────────┤
│        Application Layer        │  Commands, Queries, Validators, EventHandlers
├─────────────────────────────────┤
│          Domain Layer           │  Entities, ValueObjects, Events, Specifications
├─────────────────────────────────┤
│       Infrastructure Layer      │  EF DbContext, Repositories, External services
└─────────────────────────────────┘

Zależności: Api → Application → Domain ← Infrastructure
(Infrastructure implementuje interfejsy z Domain/Application)
```

---

# 5. Proponowana struktura frontendu React

```
workbase-web/
├── public/
├── src/
│   ├── app/                                    # App shell
│   │   ├── App.tsx
│   │   ├── Router.tsx
│   │   ├── providers/                          # Context providers (Auth, Theme, Tenant, i18n)
│   │   └── layouts/
│   │       ├── MainLayout.tsx                  # Sidebar + topbar + content
│   │       ├── AuthLayout.tsx                  # Login page
│   │       └── KioskLayout.tsx                 # Kiosk mode (fullscreen, no nav)
│   │
│   ├── shared/                                 # Shared code (web + future desktop)
│   │   ├── api/                                # API client
│   │   │   ├── client.ts                       # Axios/fetch wrapper z auth, tenant, refresh
│   │   │   ├── endpoints/                      # Typowane endpointy per moduł
│   │   │   │   ├── org.api.ts
│   │   │   │   ├── time.api.ts
│   │   │   │   ├── leave.api.ts
│   │   │   │   ├── tasks.api.ts
│   │   │   │   ├── workflow.api.ts
│   │   │   │   └── dashboard.api.ts
│   │   │   └── types/                          # Współdzielone typy API (generowane z OpenAPI)
│   │   ├── hooks/                              # Custom hooks (nie UI-specific)
│   │   │   ├── useAuth.ts
│   │   │   ├── useCurrentUser.ts
│   │   │   ├── usePermissions.ts
│   │   │   ├── useFeatureFlags.ts
│   │   │   ├── useDataScope.ts
│   │   │   └── useNotifications.ts
│   │   ├── utils/                              # Helpers
│   │   │   ├── date.ts
│   │   │   ├── format.ts
│   │   │   └── validation.ts
│   │   ├── i18n/                               # Tłumaczenia
│   │   │   ├── pl.json
│   │   │   └── en.json
│   │   └── store/                              # Global state (Zustand / TanStack Query)
│   │       ├── authStore.ts
│   │       └── notificationStore.ts
│   │
│   ├── modules/                                # Feature modules (per backend module)
│   │   ├── organization/
│   │   │   ├── pages/
│   │   │   │   ├── OrgTreePage.tsx
│   │   │   │   ├── EmployeeListPage.tsx
│   │   │   │   └── EmployeeCardPage.tsx
│   │   │   ├── components/
│   │   │   │   ├── OrgTreeView.tsx
│   │   │   │   ├── EmployeeForm.tsx
│   │   │   │   └── EmployeeCard.tsx
│   │   │   └── hooks/
│   │   │       └── useEmployees.ts
│   │   │
│   │   ├── time/
│   │   │   ├── pages/
│   │   │   │   ├── ClockInPage.tsx
│   │   │   │   ├── TimesheetPage.tsx
│   │   │   │   └── TeamAttendancePage.tsx
│   │   │   ├── components/
│   │   │   │   ├── ClockButton.tsx
│   │   │   │   ├── TimesheetTable.tsx
│   │   │   │   ├── QrScanner.tsx
│   │   │   │   └── AnomalyBadge.tsx
│   │   │   └── hooks/
│   │   │       └── useTimeTracking.ts
│   │   │
│   │   ├── leave/
│   │   │   ├── pages/
│   │   │   │   ├── LeaveRequestPage.tsx
│   │   │   │   ├── LeaveCalendarPage.tsx
│   │   │   │   └── LeaveBalancePage.tsx
│   │   │   ├── components/
│   │   │   │   ├── LeaveRequestForm.tsx
│   │   │   │   ├── LeaveCalendar.tsx
│   │   │   │   └── LeaveBalanceCard.tsx
│   │   │   └── hooks/
│   │   │
│   │   ├── tasks/
│   │   │   ├── pages/
│   │   │   │   ├── TaskListPage.tsx
│   │   │   │   ├── TaskCardPage.tsx
│   │   │   │   └── MyTasksPage.tsx
│   │   │   ├── components/
│   │   │   └── hooks/
│   │   │
│   │   ├── workflow/
│   │   │   ├── pages/
│   │   │   │   └── PendingApprovalsPage.tsx
│   │   │   └── components/
│   │   │       └── ApprovalActionBar.tsx
│   │   │
│   │   ├── dashboard/
│   │   │   ├── pages/
│   │   │   │   └── DashboardPage.tsx
│   │   │   ├── components/
│   │   │   │   ├── AttendanceWidget.tsx
│   │   │   │   ├── TaskSummaryWidget.tsx
│   │   │   │   ├── PendingApprovalsWidget.tsx
│   │   │   │   └── AlertsWidget.tsx
│   │   │   └── hooks/
│   │   │
│   │   ├── workspace/
│   │   │   ├── pages/
│   │   │   │   └── WorkspacePage.tsx           # "Co mam dziś do zrobienia"
│   │   │   └── components/
│   │   │       ├── MyDayOverview.tsx
│   │   │       ├── MyTasksList.tsx
│   │   │       ├── MyApprovalsWidget.tsx
│   │   │       └── ActivityFeed.tsx
│   │   │
│   │   ├── admin/
│   │   │   ├── pages/
│   │   │   │   ├── RolesPage.tsx
│   │   │   │   ├── PermissionsMatrixPage.tsx
│   │   │   │   ├── FeatureFlagsPage.tsx
│   │   │   │   ├── LeaveTypesConfigPage.tsx
│   │   │   │   ├── TaskStatusConfigPage.tsx
│   │   │   │   └── WorkflowConfigPage.tsx
│   │   │   └── components/
│   │   │
│   │   └── kiosk/                              # Kiosk-specific views
│   │       ├── pages/
│   │       │   └── KioskClockPage.tsx
│   │       └── components/
│   │           ├── KioskPinInput.tsx
│   │           └── KioskQrDisplay.tsx
│   │
│   └── ui/                                     # Design system / component library
│       ├── components/
│       │   ├── Button.tsx
│       │   ├── Card.tsx
│       │   ├── Table.tsx
│       │   ├── Modal.tsx
│       │   ├── Form/
│       │   ├── Timeline.tsx
│       │   ├── EntityCard.tsx                  # Generyczny renderer karty 360
│       │   └── ...
│       ├── theme/
│       │   └── tokens.ts
│       └── icons/
│
├── capacitor/                                  # Capacitor config (mobile wrapper)
│   ├── capacitor.config.ts
│   ├── android/
│   └── ios/
│
├── package.json
├── tsconfig.json
├── vite.config.ts                              # lub next.config.js — Vite rekomendowane
└── openapi-codegen.config.ts                   # Generowanie typów z OpenAPI spec
```

### 5.1. Kluczowe zasady frontendowe

| Zasada | Opis |
|---|---|
| **`shared/` = współdzielone** | Cały kod w `shared/` powinien działać w web, Capacitor (mobile) i potencjalnym Tauri (desktop). Zero zależności od DOM-specific API. |
| **`modules/` = feature-sliced** | Każdy moduł frontendowy mapuje się 1:1 na moduł backendowy. Brak importów między modułami. |
| **`ui/` = design system** | Niezależny od business logic. Można wyciągnąć jako osobny package. |
| **TanStack Query** | Zarządzanie stanem serwerowym (cache, refetch, optimistic updates). Brak Redux. |
| **Zustand** | Minimalny global state (auth, current user, notifications). |
| **React Router** | Routing z code splitting (lazy loading per module). |
| **OpenAPI codegen** | Typy API generowane z OpenAPI spec backendu → zero ręcznego typowania DTO. |
| **Responsive design** | Wszystkie widoki responsywne (desktop → tablet → mobile web). |
| **Feature flag guard** | Komponenty modułów opcjonalnych owinięte w `<FeatureGate module="cases">`. |

### 5.2. Współdzielenie kodu web ↔ mobile ↔ desktop

```
                    ┌───────────────────┐
                    │   shared/          │  API client, hooks, utils, types, i18n, store
                    │   (100% shared)    │
                    └──────┬────────────┘
                           │
              ┌────────────┼────────────────┐
              │            │                │
       ┌──────┴──────┐  ┌─┴──────────┐  ┌──┴──────────┐
       │  Web (Vite)  │  │ Mobile     │  │ Desktop     │
       │  modules/    │  │ (Capacitor)│  │ (Tauri)     │
       │  ui/         │  │ native     │  │ future      │
       │  app/        │  │ plugins    │  │             │
       └─────────────┘  └────────────┘  └─────────────┘
```

- **Web:** Pełny React app (Vite + React Router)
- **Mobile (Capacitor):** Ten sam React app opakowany w Capacitor. Dodatkowe natywne pluginy (Camera/QR, NFC, Push).
- **Desktop (Tauri, przyszłość):** Ten sam React app opakowany w Tauri. System tray, auto-start.
- **Kiosk:** Ten sam web app, ale z dedykowanym layoutem (`KioskLayout.tsx`) i ograniczoną nawigacją.

---

# 6. Model API i kontraktów

## 6.1. Konwencje REST API

| Element | Konwencja |
|---|---|
| Base URL | `/api/{module}/{resource}` |
| Wersjonowanie | Header `Api-Version: 1` (na start bez wersjonowania — wprowadzić przy public API) |
| Format | JSON (request + response) |
| Paginacja | `?page=1&pageSize=20` → response: `{ items: [...], totalCount, page, pageSize }` |
| Sortowanie | `?sortBy=createdAt&sortDir=desc` |
| Filtrowanie | `?status=open&assigneeId=xxx&search=keyword` |
| Błędy | RFC 7807 Problem Details: `{ type, title, status, detail, errors }` |
| Auth | Bearer JWT w header `Authorization: Bearer {token}` |
| Tenant | Automatycznie z JWT claim `tenant_id` (nie w URL) |
| Daty | ISO 8601 UTC (`2026-03-24T10:30:00Z`) |
| ID | UUID v7 (sortowalne chronologicznie) |

## 6.2. Standardowy response envelope

```json
// Success (single)
{
  "data": { ... }
}

// Success (list)
{
  "data": [ ... ],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 142,
    "totalPages": 8
  }
}

// Error (RFC 7807)
{
  "type": "https://workbase.app/errors/validation",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "startDate": ["Start date must be in the future."],
    "endDate": ["End date must be after start date."]
  }
}
```

## 6.3. Generowanie kontraktów

- Backend generuje **OpenAPI 3.1 spec** automatycznie (Swashbuckle / NSwag)
- Frontend generuje **TypeScript types + API client** z OpenAPI spec (openapi-typescript / orval)
- CI/CD: spec generowany przy buildzie → codegen → commit do repo frontendu (lub monorepo)

## 6.4. Rate limiting

Na start: per-tenant rate limiting (middleware w .NET). Konfigurowalny próg.
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 997
X-RateLimit-Reset: 1711276800
```

---

# 7. Auth, role, permissions, visibility

## 7.1. Przepływ autentykacji

```
┌────────┐     ┌───────────┐     ┌─────────────┐     ┌──────────┐
│ Client │────→│ Keycloak  │────→│ JWT Token   │────→│ Backend  │
│(web/mob)│←───│ OIDC/OAuth│←───│(access+     │←───│ API      │
│        │     │           │     │ refresh)    │     │          │
└────────┘     └───────────┘     └─────────────┘     └──────────┘
```

1. Client → Keycloak: Authorization Code Flow (PKCE dla SPA/mobile)
2. Keycloak → Client: JWT access token + refresh token
3. Client → Backend API: Bearer token w header
4. Backend waliduje JWT (Keycloak public key), wyciąga claims: `sub`, `tenant_id`, `roles`
5. Backend mapuje Keycloak roles → WorkBase permissions (per tenant config)

## 7.2. JWT Claims

```json
{
  "sub": "user-uuid",
  "tenant_id": "tenant-uuid",
  "email": "jan.kowalski@firma.pl",
  "roles": ["manager"],
  "employee_id": "employee-uuid",
  "org_unit_ids": ["unit-uuid-1", "unit-uuid-2"],
  "exp": 1711276800
}
```

## 7.3. Model uprawnień (RBAC + kontekst)

```
Permission = Module + Action + Scope

Przykłady:
- time.view.own          → Pracownik widzi swój czas pracy
- time.view.team         → Kierownik widzi czas pracy swojego zespołu
- time.correct.team      → Kierownik koryguje czas pracy w swoim zespole
- leave.approve.team     → Kierownik akceptuje urlopy swojego zespołu
- tasks.create.team      → Kierownik tworzy zadania w swoim zespole
- tasks.create.global    → Dyrektor tworzy zadania globalnie
- admin.roles.manage     → Admin zarządza rolami
- admin.features.manage  → Super Admin włącza/wyłącza moduły
```

### Tabela permissions (schemat):

```sql
CREATE TABLE permissions (
    id UUID PRIMARY KEY,
    module VARCHAR(50) NOT NULL,       -- 'time', 'leave', 'tasks', 'workflow', 'admin'
    action VARCHAR(50) NOT NULL,       -- 'view', 'create', 'edit', 'delete', 'approve', 'correct', 'manage'
    scope VARCHAR(20) NOT NULL,        -- 'own', 'team', 'department', 'global'
    description TEXT
);

CREATE TABLE role_permissions (
    role_id UUID REFERENCES roles(id),
    permission_id UUID REFERENCES permissions(id),
    PRIMARY KEY (role_id, permission_id)
);
```

## 7.4. Mechanizm Data Scope (widoczność danych)

Middleware `DataScopeMiddleware`:
1. Z JWT wyciąga `user_id`, `roles`, `org_unit_ids`
2. Z konfiguracji ról per tenant wyciąga scope per moduł
3. Ustawia kontekst `IDataScopeContext` dostępny w requestie:

```csharp
public interface IDataScopeContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    Guid EmployeeId { get; }
    DataScopeLevel Level { get; } // Own, Team, Department, Global
    IReadOnlyList<Guid> VisibleUnitIds { get; } // jednostki org. w scope
}
```

4. Repository / query automatycznie filtruje dane wg `VisibleUnitIds` lub `EmployeeId`

```csharp
// Przykład: automatyczne filtrowanie w query
public async Task<PagedResult<TaskDto>> GetTasks(GetTasksQuery query, IDataScopeContext scope)
{
    var tasks = dbContext.Tasks
        .Where(t => t.TenantId == scope.TenantId)
        .Where(t => scope.Level == DataScopeLevel.Own
            ? t.AssigneeId == scope.EmployeeId
            : scope.VisibleUnitIds.Contains(t.AssigneeUnitId));
    // ...
}
```

## 7.5. Feature Flag Check

Middleware `FeatureFlagMiddleware`:
- Sprawdza, czy żądany endpoint należy do modułu, który jest włączony dla danego tenanta
- Jeśli moduł wyłączony → 404 Not Found (moduł nie istnieje)

```csharp
// Na controller:
[RequireModule("cases")]
public class CasesController : ControllerBase { ... }
```

---

# 8. Baza danych i podział logiczny

## 8.1. Strategia multi-tenancy

**MVP: Shared Database + tenant_id**

```
┌──────────────────────────────────┐
│          PostgreSQL               │
│   ┌──────────────────────────┐   │
│   │    Schema: public         │   │
│   │                           │   │
│   │  employees (tenant_id)    │   │
│   │  tasks (tenant_id)        │   │
│   │  time_entries (tenant_id) │   │
│   │  ...                      │   │
│   └──────────────────────────┘   │
└──────────────────────────────────┘
```

Każda tabela biznesowa ma kolumnę `tenant_id UUID NOT NULL` z indeksem.

EF Core global query filter:
```csharp
modelBuilder.Entity<Employee>().HasQueryFilter(e => e.TenantId == _currentTenant.Id);
```

**Przyszłość (jeśli potrzeba izolacji):** Schema per tenant → `tenant_abc.employees`, `tenant_xyz.employees`

## 8.2. Logiczny podział tabel per moduł

| Moduł | Prefix tabel | Tabele główne |
|---|---|---|
| Organization | `org_` | `org_tenants`, `org_units`, `org_unit_types`, `org_unit_closure`, `org_positions`, `org_employees`, `org_employee_assignments`, `org_supervisor_relations` |
| Identity | `iam_` | `iam_users`, `iam_roles`, `iam_permissions`, `iam_role_permissions`, `iam_user_roles`, `iam_data_scopes`, `iam_feature_flags` |
| Time | `time_` | `time_entries`, `time_sheets`, `time_schedules`, `time_schedule_templates`, `time_anomalies`, `time_corrections`, `time_qr_tokens` |
| Leave | `leave_` | `leave_types`, `leave_policies`, `leave_balances`, `leave_requests`, `leave_decisions`, `leave_calendar_entries` |
| Tasks | `task_` | `task_tasks`, `task_statuses`, `task_priorities`, `task_status_transitions`, `task_comments`, `task_attachments`, `task_history`, `task_reminders` |
| Workflow | `wf_` | `wf_definitions`, `wf_instances`, `wf_steps`, `wf_transitions`, `wf_approval_requests`, `wf_approval_decisions`, `wf_actions`, `wf_escalation_rules` |
| Dashboard | — | Brak tabel własnych (read-only, agreguje z innych modułów). `dash_configs`, `dash_widgets` dla konfiguracji. |
| Notification | `notif_` | `notif_notifications`, `notif_templates`, `notif_preferences` |
| Documents | `doc_` | `doc_documents`, `doc_categories`, `doc_expiries` |
| Audit | `audit_` | `audit_entries` (append-only, brak UPDATE/DELETE) |

## 8.3. Custom Fields (JSONB)

Strategia: Kolumna `custom_fields JSONB` na tabelach konfigurowalnych.

```sql
ALTER TABLE org_employees ADD COLUMN custom_fields JSONB DEFAULT '{}';

-- Definicja dostępnych pól per tenant:
CREATE TABLE cfg_custom_field_definitions (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    entity_type VARCHAR(50) NOT NULL,  -- 'employee', 'task', 'case', 'contact'
    field_key VARCHAR(100) NOT NULL,
    field_label VARCHAR(200) NOT NULL,
    field_type VARCHAR(50) NOT NULL,   -- 'text', 'number', 'date', 'select', 'multiselect', 'boolean'
    options JSONB,                      -- dla select/multiselect
    is_required BOOLEAN DEFAULT FALSE,
    sort_order INT DEFAULT 0,
    UNIQUE (tenant_id, entity_type, field_key)
);
```

**Indexing:** GIN index na `custom_fields` dla wyszukiwania.

```sql
CREATE INDEX idx_employees_custom_fields ON org_employees USING GIN (custom_fields);
```

## 8.4. Konfiguracja per tenant

```sql
CREATE TABLE cfg_tenant_configs (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    module VARCHAR(50) NOT NULL,
    config_key VARCHAR(100) NOT NULL,
    config_value JSONB NOT NULL,
    UNIQUE (tenant_id, module, config_key)
);
```

Przykłady config_value:
- `("time", "anomaly_rules")` → `{"late_threshold_minutes": 15, "max_shift_hours": 12}`
- `("leave", "conflict_threshold")` → `{"max_simultaneous_percent": 30}`
- `("tasks", "statuses")` → `[{"id": "new", "name": "Nowe", "color": "#blue"}, ...]`

## 8.5. Indeksowanie

Kluczowe indeksy:
```sql
-- Multi-tenancy (na KAŻDEJ tabeli):
CREATE INDEX idx_{table}_tenant ON {table} (tenant_id);

-- Filtry per scope:
CREATE INDEX idx_employees_unit ON org_employees (tenant_id, org_unit_id);
CREATE INDEX idx_tasks_assignee ON task_tasks (tenant_id, assignee_id, status);
CREATE INDEX idx_time_entries_employee ON time_entries (tenant_id, employee_id, date);
CREATE INDEX idx_leave_requests_status ON leave_requests (tenant_id, status, approver_id);

-- Audyt (append-only, query by entity):
CREATE INDEX idx_audit_entity ON audit_entries (tenant_id, entity_type, entity_id);
CREATE INDEX idx_audit_user ON audit_entries (tenant_id, user_id, created_at);

-- Closure table:
CREATE INDEX idx_closure_ancestor ON org_unit_closure (ancestor_id);
CREATE INDEX idx_closure_descendant ON org_unit_closure (descendant_id);
```

## 8.6. Migracje

- EF Core Migrations per moduł (osobne `DbContext` lub osobne migration assemblies)
- Migracje run at startup (lub jako osobny step w CI/CD)
- Seed data: predefiniowane role (Super Admin, Admin), predefiniowane typy jednostek, predefiniowane typy nieobecności
- Rollback strategy: migracje powinny mieć `Down()` method

---

# 9. Audyt, logi i observability

## 9.1. Audyt trail (biznesowy)

**Tabela `audit_entries`** — append-only:

```sql
CREATE TABLE audit_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    user_id UUID NOT NULL,
    employee_id UUID,
    action VARCHAR(100) NOT NULL,       -- 'employee.created', 'task.status_changed', 'leave.approved'
    entity_type VARCHAR(50) NOT NULL,   -- 'Employee', 'Task', 'LeaveRequest'
    entity_id UUID NOT NULL,
    old_values JSONB,                   -- { "status": "new" }
    new_values JSONB,                   -- { "status": "in_progress" }
    metadata JSONB,                     -- { "ip": "...", "user_agent": "...", "comment": "..." }
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- BRAK instrukcji UPDATE/DELETE na tej tabeli.
-- Revoke update, delete privileges na poziomie DB.
REVOKE UPDATE, DELETE ON audit_entries FROM workbase_app;
```

**Implementacja:** EF Core `SaveChangesInterceptor` automatycznie przechwytuje zmiany.

```csharp
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        var entries = context.ChangeTracker.Entries<IAuditable>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var entry in entries)
        {
            var auditEntry = new AuditEntry
            {
                TenantId = currentTenant.Id,
                UserId = currentUser.Id,
                Action = DetermineAction(entry),
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id,
                OldValues = entry.State == EntityState.Modified ? GetOriginalValues(entry) : null,
                NewValues = GetCurrentValues(entry),
            };
            context.Set<AuditEntry>().Add(auditEntry);
        }
        // ...
    }
}
```

## 9.2. Logi techniczne (Serilog)

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", "WorkBase")
    .Enrich.FromLogContext()                  // RequestId, TenantId, UserId
    .WriteTo.Console(new JsonFormatter())     // Dev: console
    .WriteTo.Seq("http://seq:5341")           // Staging/Prod: Seq
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .CreateLogger();
```

**Enrichment per request:**
```csharp
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TenantId", httpContext.GetTenantId());
        diagnosticContext.Set("UserId", httpContext.GetUserId());
        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
    };
});
```

## 9.3. Health checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddRedis(redisConnection)           // jeśli cache
    .AddHangfire()
    .AddUrlGroup(new Uri(keycloakUrl))   // Keycloak health
    .AddSignalRHub();
```

Endpoint: `GET /health` — health check (bez auth)

## 9.4. Metryki (future-ready)

Na MVP: Serilog + Seq + health checks.
Na post-MVP: OpenTelemetry → Prometheus + Grafana (traces, metrics, logs).

Przygotowanie: OpenTelemetry SDK dodany jako zależność w Host, konfiguracja wyłączona za feature flag.

---

# 10. Mobile Architecture

## 10.1. Strategia

**MVP: Capacitor (web app opakowany w natywny shell)**

```
┌────────────────────────────────┐
│         React Web App          │  (ten sam kod co web)
├────────────────────────────────┤
│       Capacitor Runtime        │  (bridge JS ↔ Native)
├────────────────────────────────┤
│  Native Plugins                │
│  ├── Camera (QR scan)          │
│  ├── NFC (post-MVP)            │
│  ├── Push Notifications (FCM)  │
│  └── Geolocation (opcjonalnie) │
├────────────────────────────────┤
│   Android / iOS                │
└────────────────────────────────┘
```

**Dlaczego Capacitor a nie React Native:**
- Współdzielenie 95%+ kodu z webem (ten sam React, te same hooki, te same komponenty)
- Natywny dostęp do Camera (QR), NFC, Push, Geolocation
- Znacznie mniejszy koszt utrzymania niż osobna aplikacja React Native
- Wystarczająca wydajność dla naszych use case'ów (formularze, listy, dashboardy)

**Kiedy rozważyć React Native:**
- Jeśli wydajność Capacitor będzie niewystarczająca (np. heavy animations)
- Jeśli pojawią się natywne wymagania niemożliwe przez Capacitor plugins

## 10.2. Specyfika mobilna

Co jest inne na mobile:
- Layout — uproszczona nawigacja (bottom tabs, nie sidebar)
- Clock-in — camera/QR scan jako primary action
- Push notifications — FCM (Firebase Cloud Messaging)
- Offline — na MVP brak. Post-MVP: basic offline queue (Capacitor Storage + sync on reconnect)

**Routing mobilny:**

| Tab | Zawartość | Odpowiednik web |
|---|---|---|
| Mój dzień | Clock-in/out, status, workspace | WorkspacePage |
| Zadania | Lista moich zadań | MyTasksPage |
| Wnioski | Moje wnioski + akceptacje | LeaveRequestPage + PendingApprovalsPage |
| Więcej | Profil, ustawienia | EmployeeCardPage |

## 10.3. QR Code flow (mobile)

```
MVP QR flow:

1. System generuje QR token per punkt rejestracji (kiosk/tablet)
   → QR wyświetlany na kiosku, odświeżany co X sekund (rolling token)

2. Pracownik otwiera mobile app → kamera → skan QR

3. App wysyła: POST /api/time/qr/verify
   → body: { qrToken, employeeId, method: "qr", location?: {...} }

4. Backend waliduje token (ważność, tenant, lokacja) → rejestruje clock-in

Alternatywnie:
- QR generowany PER PRACOWNIKA (na ekranie telefonu) → skanowany przez kiosk
- Ten wariant wymaga kamery na kiosku — do decyzji.
```

## 10.4. API design mobile-friendly

- Endpoint `/api/time/today` zwraca kompaktowy obiekt (nie full timesheet)
- Endpoint `/api/workspace/my-day` — dedykowany agregat dla widoku mobilnego
- Pagination z sensownym `pageSize` (20, nie 100)
- Sparse fields: `?fields=id,title,status,deadline` (opcjonalnie, post-MVP)
- Compression: gzip/brotli w responses

---

# 11. Desktop / Kiosk Future Readiness

## 11.1. Desktop (Tauri) — przyszłość

**Nie budujemy na MVP.** Ale architektura to umożliwia:

- Ten sam React app (z `shared/` + `modules/`) opakowany w Tauri
- Tauri daje: system tray, auto-start, natywne powiadomienia OS, mniejszy bundle niż Electron
- Dodatkowe feature'y desktop:
  - Ikona tray z clock-in/out statusem
  - Powiadomienia natywne OS
  - Auto-start z systemem operacyjnym
  - Idle detection (auto-pause po braku aktywności — opcjonalnie)

**Co robimy teraz, żeby to umożliwić:**
1. `shared/` nie używa API specyficznego dla przeglądarki (np. `window.location` — abstrakcja)
2. API client nie zakłada cookies — JWT bearer token
3. State management nie zależy od browser storage — abstrakcja storage
4. Routing obsługuje zarówno browser history jak i hash routing (Tauri używa hash)

## 11.2. Kiosk mode — MVP

Kiosk to **web app w trybie pełnoekranowym** na tablecie/terminalu.

Architektura:
- Ten sam React app z dedykowanym `KioskLayout.tsx`
- Route: `/kiosk` — wymaga konfiguracji per punkt rejestracji
- Brak pełnej nawigacji — tylko ekran clock-in/out
- Identyfikacja pracownika: PIN / QR osobiste / NFC badge
- Auto-refresh co X minut (heartbeat)
- Brak logout (kiosk jest zalogowany na konto systemowe "kiosk-{location}")

```
/kiosk?locationId=xxx&mode=qr

Ekran:
┌──────────────────────────────┐
│  WorkBase          09:15:32  │
│                              │
│   Przyłóż kartę / Wpisz PIN │
│                              │
│     ┌──────────────────┐     │
│     │   [PIN: ____]    │     │
│     └──────────────────┘     │
│                              │
│   lub                        │
│                              │
│     ┌──────────────────┐     │
│     │   [Skanuj QR]    │     │
│     └──────────────────┘     │
│                              │
│  Ostatnie odbicia:           │
│  Jan K.   ✅ 08:02           │
│  Anna M.  ✅ 08:05           │
└──────────────────────────────┘
```

---

# 12. Integracje zewnętrzne

## 12.1. Architektura integracji

Integracje są wydzielone do `MOD-INTEGRATIONS` z adapter pattern:

```
┌──────────────────────────────────────────────┐
│           MOD-INTEGRATIONS                    │
│                                               │
│  ┌──────────────┐  ┌──────────────────────┐  │
│  │ Integration   │  │ Integration Config    │  │
│  │ Service       │  │ (per tenant)          │  │
│  │ (orchestrator)│  │                       │  │
│  └──────┬───────┘  └──────────────────────┘  │
│         │                                     │
│  ┌──────┴──────────────────────────────────┐ │
│  │           Adapter Interface              │ │
│  │  ICalendarAdapter                        │ │
│  │  IEmailAdapter                           │ │
│  │  IMessagingAdapter                       │ │
│  └──────┬─────────────┬──────────┬─────────┘ │
│         │             │          │            │
│  ┌──────┴──┐  ┌──────┴──┐  ┌───┴────────┐  │
│  │ Google  │  │Microsoft│  │   Slack     │  │
│  │ Adapter │  │ Adapter │  │   Adapter   │  │
│  └─────────┘  └─────────┘  └────────────┘  │
└──────────────────────────────────────────────┘
```

## 12.2. OAuth2 token management

```sql
CREATE TABLE int_tokens (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    user_id UUID NOT NULL,
    provider VARCHAR(50) NOT NULL,       -- 'google', 'microsoft', 'slack'
    access_token TEXT NOT NULL,          -- encrypted at rest
    refresh_token TEXT,                   -- encrypted at rest
    expires_at TIMESTAMPTZ,
    scopes TEXT[],
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

**Encryption:** tokeny szyfrowane w bazie (AES-256, klucz w secrets/vault).

## 12.3. Sync patterns

| Integracja | Kierunek | Metoda | Częstotliwość |
|---|---|---|---|
| Google Calendar ← WorkBase | Push (WorkBase → Calendar) | Google Calendar API | Event-driven (urlop approved → create calendar event) |
| Google Calendar → WorkBase | Pull | Google Calendar API | Cron co 15 min (opcjonalnie) |
| Outlook Calendar | Analogicznie jak Google | Microsoft Graph API | j.w. |
| Gmail/Outlook → WorkBase | Pull (sidebar plugin) | Gmail API / Graph API | On-demand (user opens sidebar) |
| Slack ← WorkBase | Push (notifications) | Slack Webhook / Bot | Event-driven |

## 12.4. Etap realizacji

- MVP: Brak integracji zewnętrznych (poza Keycloak)
- Post-MVP: Google Calendar sync, Outlook Calendar sync, Slack webhooks
- Premium: Gmail sidebar, Teams bot, full 2-way sync

---

# 13. Background jobs i procesy asynchroniczne

## 13.1. Hangfire — konfiguracja

```csharp
services.AddHangfire(config => config
    .UsePostgreSqlStorage(connectionString)
    .UseSerializerSettings(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None }));

services.AddHangfireServer(options =>
{
    options.Queues = new[] { "critical", "default", "reports" };
    options.WorkerCount = Environment.ProcessorCount * 2;
});
```

## 13.2. Typy jobów

| Typ | Przykłady | Queue | Scheduler |
|---|---|---|---|
| **Cron (recurring)** | Wykrywanie anomalii czasu pracy, sprawdzanie deadline'ów zadań, document expiry check, leave balance recalculation | `default` | Cron expressions |
| **Delayed** | Przypomnienie o task deadline (za 24h), escalacja workflow (za 48h), reminder o akceptacji | `default` | Fire-and-forget z delay |
| **Event-driven** | Wyślij email po approval, utwórz calendar entry, aktualizuj saldo urlopowe | `critical` | Triggered by domain events |
| **Reports** | Generowanie raportu zbiorczego, eksport CSV dużych zbiorów | `reports` | On-demand |

## 13.3. Hangfire + multi-tenancy

Każdy job musi mieć `tenantId` w parametrach. Job executor ustawia kontekst tenanta przed wykonaniem.

```csharp
public class TenantAwareJobFilter : IClientFilter, IServerFilter
{
    public void OnCreating(CreatingContext context)
    {
        context.SetJobParameter("TenantId", _currentTenant.Id);
    }

    public void OnPerforming(PerformingContext context)
    {
        var tenantId = context.GetJobParameter<Guid>("TenantId");
        _tenantContext.Set(tenantId);
    }
}
```

## 13.4. Retries i dead letters

- Automatic retries: 3 attempts z exponential backoff
- Dead letter: po 3 failure'ach → log error + alert do admina
- Hangfire Dashboard: dostępny pod `/hangfire` (auth required, admin only)

---

# 14. DevOps i środowiska

## 14.1. Środowiska

| Środowisko | Cel | Infra |
|---|---|---|
| **Local (dev)** | Developerski | `docker-compose.dev.yml` — PostgreSQL, Keycloak, MinIO, Seq, Hangfire |
| **CI** | Testy automatyczne | GitHub Actions — build, test, lint, migration check |
| **Staging** | Pre-production, demo | Docker na VPS / cloud VM — pełny stack |
| **Production** | Produkcja | Docker na VPS / cloud VM (na start), Kubernetes (przyszłość) |

## 14.2. docker-compose.dev.yml

```yaml
services:
  postgres:
    image: postgres:16
    ports: ["5432:5432"]
    environment:
      POSTGRES_DB: workbase
      POSTGRES_USER: workbase
      POSTGRES_PASSWORD: dev_password
    volumes: [pgdata:/var/lib/postgresql/data]

  keycloak:
    image: quay.io/keycloak/keycloak:24.0
    ports: ["8080:8080"]
    environment:
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://postgres/keycloak
      KC_DB_USERNAME: workbase
      KC_DB_PASSWORD: dev_password
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
    command: start-dev
    depends_on: [postgres]

  minio:
    image: minio/minio
    ports: ["9000:9000", "9001:9001"]
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin

  seq:
    image: datalust/seq
    ports: ["5341:5341", "8081:80"]
    environment:
      ACCEPT_EULA: Y

  workbase-api:
    build:
      context: .
      dockerfile: Dockerfile
    ports: ["5000:5000"]
    depends_on: [postgres, keycloak, minio]
    environment:
      ConnectionStrings__Default: "Host=postgres;Database=workbase;Username=workbase;Password=dev_password"
      Keycloak__Authority: http://keycloak:8080/realms/workbase
      Storage__Endpoint: minio:9000
      Serilog__WriteTo__Seq__ServerUrl: http://seq:5341

volumes:
  pgdata:
```

## 14.3. CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/ci.yml
name: CI

on: [push, pull_request]

jobs:
  backend:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env: { POSTGRES_DB: workbase_test, POSTGRES_PASSWORD: test }
        ports: ['5432:5432']
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '9.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
      - run: dotnet publish -c Release -o ./publish

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: npm ci
        working-directory: workbase-web
      - run: npm run lint
        working-directory: workbase-web
      - run: npm run type-check
        working-directory: workbase-web
      - run: npm run test
        working-directory: workbase-web
      - run: npm run build
        working-directory: workbase-web

  deploy-staging:
    needs: [backend, frontend]
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - run: # docker build + docker push + deploy to staging
```

## 14.4. Branching strategy

| Branch | Cel |
|---|---|
| `main` | Stabilny kod, deploy na staging automatycznie |
| `release/*` | Release candidate, deploy na produkcję |
| `feature/*` | Feature branches, PR do main |
| `hotfix/*` | Hotfixy produkcyjne, PR do main + release |

## 14.5. Secrets management

- **Dev:** `.env` file (git-ignored) + docker-compose environment
- **CI:** GitHub Secrets
- **Staging/Prod:** Environment variables na serwerze (docelowo: HashiCorp Vault lub Azure Key Vault)

Wrażliwe dane: Keycloak secret, DB connection string, MinIO keys, email credentials, integration OAuth secrets, encryption keys.

---

# 15. Ryzyka techniczne

| # | Ryzyko | Prawdopodobieństwo | Wpływ | Mitygacja |
|---|---|---|---|---|
| RT1 | **Workflow engine zbyt uproszczony** — nie obsłuży złożonych scenariuszy | Średnie | Wysoki | Zaprojektować silnik jako generyczny od razu. JSON definitions, strategy pattern. Ewaluacja Elsa Workflows jako alternatywy. |
| RT2 | **Performance data scope** — query z filtrami widoczności wolne na dużych zbiorach | Średnie | Średni | Indeksy na `(tenant_id, org_unit_id)`. Denormalizacja `unit_id` tam gdzie potrzeba. Dapper dla dashboardy. |
| RT3 | **Keycloak complexity** — learning curve, konfiguracja realma, customizacja | Średnie | Średni | Gotowy realm config w repo. Automatyczny provisioning. Dedykowany czas na spike. |
| RT4 | **Custom fields performance** — JSONB z GIN indexem na dużej skali | Niskie | Średni | Monitoring query performance. Plan B: EAV table dla heavy querying. |
| RT5 | **Capacitor limitations** — NFC, push, camera mogą nie działać idealnie | Średnie | Średni | Spike na NFC + Camera wcześnie. Plan B: PWA (bez NFC). |
| RT6 | **Multi-tenant data leak** — bug w query filter → dane innego tenanta widoczne | Niskie | Krytyczny | Global query filter w EF Core. Architecture tests (ArchTests) sprawdzające tenant filter. Security review. Integration tests. |
| RT7 | **Migration complexity** — wiele DbContextów, per-module migrations, ordering | Średnie | Średni | Jeden główny DbContext z konfiguracjami per moduł. Migration tests w CI. |
| RT8 | **SignalR na skali** — WebSocket connections per server | Niskie (MVP) | Niski (MVP) | Na MVP wystarczy in-process. Post-MVP: Redis backplane. |
| RT9 | **Hangfire scheduler leak** — job zaprojektowany bez tenant context | Średnie | Wysoki | `TenantAwareJobFilter` jako global filter. Code review checklist. |
| RT10 | **Frontend monolith React** — duży bundle, wolne ładowanie | Średnie | Średni | Code splitting per module (React.lazy). Tree shaking. Vite. |

---

# 16. Decyzje architektoniczne do potwierdzenia

## 16.1. Do podjęcia PRZED rozpoczęciem kodowania

| # | Decyzja | Opcje | Rekomendacja | Status |
|---|---|---|---|---|
| AD1 | **DbContext: jeden vs. per moduł** | (a) Jeden główny z konfiguracjami per moduł, (b) Osobne DbContexty per moduł | **(a) Jeden główny** — prostsze migracje, prostszy tenant filter. Per-module configurations via `IEntityTypeConfiguration`. | Do potwierdzenia |
| AD2 | **UUID: v4 vs. v7** | (a) UUID v4 (random), (b) UUID v7 (time-sortable) | **(b) UUID v7** — sortowalny chronologicznie, lepszy performance w B-tree index. Wymaga .NET 9+ lub custom generator. | Do potwierdzenia |
| AD3 | **Mediator: MediatR vs. własny** | (a) MediatR, (b) Własny lightweight | **(a) MediatR** — dojrzały, pipeline behaviors (logging, validation, auth). | Do potwierdzenia |
| AD4 | **Validation: FluentValidation vs. Data Annotations** | (a) FluentValidation, (b) Data Annotations | **(a) FluentValidation** — bardziej elastyczny, testable, separation of concerns. | Do potwierdzenia |
| AD5 | **Frontend state: TanStack Query + Zustand vs. Redux Toolkit** | (a) TanStack Query + Zustand, (b) RTK Query + Redux | **(a) TanStack Query + Zustand** — mniejszy boilerplate, server-state vs. client-state separation. | Do potwierdzenia |
| AD6 | **Frontend build: Vite vs. Next.js** | (a) Vite (SPA), (b) Next.js (SSR/SSG) | **(a) Vite SPA** — SaaS app nie potrzebuje SSR. Prostsze, szybsze buildy. Kompatybilne z Capacitor/Tauri. | Do potwierdzenia |
| AD7 | **CSS: Tailwind vs. CSS Modules vs. styled-components** | (a) Tailwind CSS, (b) CSS Modules, (c) styled-components | **(a) Tailwind CSS** — szybki development, mały bundle, utility-first. | Do potwierdzenia |
| AD8 | **UI Component library: custom vs. Shadcn/ui vs. Ant Design** | (a) Custom, (b) Shadcn/ui, (c) Ant Design, (d) MUI | **(b) Shadcn/ui** — bazuje na Radix, copy-paste (pełna kontrola), Tailwind-native, nie dodaje dependency. | Do potwierdzenia |
| AD9 | **Mobile: Capacitor vs. PWA-only na MVP** | (a) Capacitor od MVP, (b) PWA na MVP, Capacitor post-MVP | **(b) PWA na MVP** — szybciej. Capacitor jeśli QR/NFC krytyczne. Zależy od pilota. | Do potwierdzenia |
| AD10 | **Workflow definitions: JSON schema vs. relational tables** | (a) JSON column + schema validation, (b) Relacyjne tabele per element | **(a) JSON** — elastyczniejsze, łatwiejsze wersjonowanie, łatwiejsze UI edytora. Relacyjne tabele dla instancji runtime. | Do potwierdzenia |

## 16.2. Do podjęcia PRZED post-MVP

| # | Decyzja | Opis |
|---|---|---|
| AD11 | **Redis: czy potrzebny?** | Cache (session, rate limit, SignalR backplane). Na MVP in-memory wystarczy. Redis jeśli >1 instancja backendu. |
| AD12 | **Message broker: kiedy?** | Na MVP: in-process domain events. Jeśli wydzielamy serwis → RabbitMQ / Azure Service Bus. |
| AD13 | **Full-text search: PostgreSQL vs. Elasticsearch** | Na MVP: PostgreSQL `tsvector` + `ts_query`. Elasticsearch jeśli potrzeba semantycznego search (AI module). |
| AD14 | **Blob encryption** | Czy pliki w MinIO szyfrowane at rest? Rekomendacja: tak (MinIO server-side encryption). |

---

## Podsumowanie: co jest frameworkiem/mechanizmem, a co twardą implementacją

| Element | Framework/Mechanizm | Twarda implementacja |
|---|---|---|
| Struktura organizacyjna | ✅ Closure table + konfigurowalne typy | ❌ Nie hardkodujemy poziomów |
| Role i uprawnienia | ✅ RBAC engine + matryca | ❌ Nie hardkodujemy ról |
| Data scope | ✅ Middleware + query filter | ❌ Nie hardkodujemy reguł widoczności |
| Statusy zadań/spraw | ✅ Konfigurowalny state machine | ❌ Nie hardkodujemy statusów |
| Workflow/approval | ✅ JSON-driven engine | ❌ Nie kodujemy per typ procesu |
| Formularze | ✅ (post-MVP) Form builder | ✅ (MVP) Predefiniowane szablony |
| Custom fields | ✅ JSONB + field definitions | — |
| Audyt | ✅ EF interceptor + append-only table | — |
| Powiadomienia | ✅ Event-driven dispatcher + szablony | — |
| Dashboard | ✅ Konfigurowalny per rola (widżety) | ✅ (MVP) Predefiniowane widżety |
| Anomalie czasu pracy | ✅ Reguły konfigurowalne per tenant | ❌ Nie hardkodujemy progów |
| Integracje | ✅ Adapter pattern per provider | — |
| Feature flags | ✅ Module registry per tenant | — |
| Karty 360 | ✅ Generyczny entity card renderer | ❌ Nie osobny kod per typ karty |

---

> **Następny krok:** Na podstawie tego dokumentu przejść do szczegółowego backlogu tasków implementacyjnych per faza MVP (docs/04-...).
