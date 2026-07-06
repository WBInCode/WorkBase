# WorkBase — Audyt projektu i mapa wiedzy

> Dokument audytowy wygenerowany na podstawie pełnego przeglądu repozytorium (backend .NET, frontend React, dokumentacja, infrastruktura).
> Data audytu: 2026-07-03 · Zakres: spójność, logika, jakość kodu, założenia, martwy kod, bezpieczeństwo, potencjał rozwojowy/biznesowy, funkcjonalność.
> **Status realizacji poprawek:** patrz sekcja [8. Log wykonanych poprawek](#8-log-wykonanych-poprawek) na końcu dokumentu.

---

## 1. Czym jest WorkBase — mapa wiedzy o projekcie

**Kategoria:** Operacyjno-zarządcza platforma SaaS B2B (workforce / operations management, nie klasyczny sprzedażowy CRM).

**Wizja produktu:** jeden system łączący HR, operacje i zarządzanie — czas pracy, zadania, workflow (akceptacje), urlopy i dashboard kierowniczy. Przeciwwaga dla silosów typu Salesforce (sprzedaż) czy Calamari (tylko HR).

**Rynek docelowy:** firmy 5–2000+ pracowników, branżowo-agnostyczne.

| Segment | Profil | Oczekiwania |
|---|---|---|
| Małe (5–30) | Płaska struktura | Szybki start, minimum konfiguracji |
| Średnie (30–200) | Działy, zespoły | Workflowy, hierarchia ról, raporty |
| Duże (200–2000+) | Multi-site | Struktura oddziałów, zaawansowane uprawnienia |

**Model biznesowy:** multi-tenant SaaS z licencjonowaniem przez feature flags. Rdzeń (czas + zadania + workflow) w tierze bazowym; case management, sprzedaż, AI, moduły dziedzinowe jako premium. Konkretny cennik per-seat — jeszcze niezdefiniowany w dokumentacji.

**Zasady produktowe:** konfiguracja zamiast kodu · multi-tenant domyślnie · modularny ale spójny · zorientowany na zarządzanie · audytowalność (append-only).

### Stack technologiczny (stan faktyczny)

| Warstwa | Zadeklarowane (README) | Stan faktyczny w repo |
|---|---|---|
| Backend | .NET 9, ASP.NET Core, EF Core 9, Dapper | ✅ Zgodne, w pełni użyte |
| CQRS | MediatR | ✅ Pełne (ICommand/IQuery + behaviors) |
| Frontend | React 19, TS, Vite, TanStack Query, **Zustand, Tailwind, Shadcn/ui** | ⚠️ React 19, TS, Vite, TanStack Query — TAK; **Zustand, Tailwind, Shadcn — NIE (nieużyte / brak)** |
| DB | PostgreSQL 16+ | ✅ |
| Auth | Keycloak 24 (OIDC/JWT/RBAC) | ✅ Skonfigurowane |
| Storage | MinIO (S3) | ✅ |
| Logging | Serilog → Seq | ✅ |
| Jobs | Hangfire (PostgreSQL) | ✅ (dashboard z niedokończoną auth) |
| Real-time | SignalR | ✅ |
| Mobile | Capacitor / PWA | ⚠️ Skonfigurowane, nietestowane |
| Desktop | Tauri 2 (`src-tauri/`) | ⚠️ Szkielet, brak realnego kodu (`lib.rs` pusty) |

### Architektura

- **Modular monolith** — 15 modułów, każdy w 4 warstwach `.{Domain|Application|Infrastructure|Api}`.
- **CQRS light** przez MediatR; **Result pattern** wszędzie; **Domain Events** in-process do komunikacji między modułami.
- **Multi-tenancy**: shared DB + `tenant_id` + globalny filtr zapytań EF Core.
- **RBAC + Data Scope**: role, uprawnienia, zakres widoczności per rola.
- Testy architektoniczne (NetArchTest) pilnują braku referencji krzyżowych między modułami.

---

## 2. Stan implementacji modułów

Kod domenowy **jest realnie zaimplementowany** (~658 plików `.cs` w `src/Modules/`) — nie są to szkielety. To istotna korekta względem samej dokumentacji, która sugerowała wczesny etap.

| Moduł | Zaimplementowany | Wpięty do aplikacji (`ModuleDiscovery`) | Frontend | Uwaga |
|---|---|---|---|---|
| Organization | ✅ | ✅ | ✅ | Rdzeń, dojrzały |
| Identity (IAM) | ✅ | ✅ | ⚠️ częściowo (role/permissions/flags) | |
| TimeTracking | ✅ | ✅ | ✅ | Biometria, geofence, anomalie |
| Leave | ✅ | ✅ | ✅ | |
| Tasks | ✅ | ✅ | ✅ | |
| Workflow | ✅ | ✅ | ✅ (builder) | Silnik akceptacji, eskalacje |
| Dashboard | ✅ | ✅ | ✅ | Dapper read-side |
| Notification | ✅ | ✅ | ✅ | SignalR + email |
| Documents | ✅ | ✅ | ✅ | |
| Integration | ✅ | ✅ | ❌ | Adaptery Google/MS/Slack/Teams |
| **AI** | ✅ | 🔴 **NIE** | ❌ | OpenAI summarize/classify |
| **Cases** | ✅ | 🔴 **NIE** | ❌ | |
| **Contacts** | ✅ | 🔴 **NIE** | ❌ | |
| **Forms** | ✅ | 🔴 **NIE** | ❌ | Dynamic form builder |
| **Sales** | ✅ | 🔴 **NIE** | ❌ | Lead→Opportunity→Offer |

**Kluczowe odkrycie:** 5 modułów (AI, Cases, Contacts, Forms, Sales) ma pełny kod, ale **nie są zarejestrowane** — patrz problem #1 poniżej.

---

## 3. Co jest problemem (do naprawy)

### 🔴 KRYTYCZNE

#### ✅ P1. [NAPRAWIONE] 5 modułów jest „osieroconych" — kod istnieje, ale aplikacja go nie ładuje
`src/WorkBase.Infrastructure/ModuleDiscovery.cs` (linie 9–31) trzyma **zahardkodowaną listę** 10 modułów (× Infrastructure + Api). Brakuje **AI, Cases, Contacts, Forms, Sales** — a `FindImplementations<T>()` skanuje tylko te wymienione assembly.
- **Skutek:** serwisy DI i endpointy tych 5 modułów nie są rejestrowane. Cały ich kod to de facto martwy kod w runtime.
- **Naprawa:** przejść na konwencyjne auto-wykrywanie:
  ```csharp
  var assemblies = AppDomain.CurrentDomain.GetAssemblies()
      .Where(a => a.GetName().Name?.StartsWith("WorkBase.Modules.") == true);
  ```
  lub dopisać brakujące moduły do listy. Uwaga: auto-discovery wymaga, by referencje assembly były faktycznie załadowane (dołożyć marker-typy lub jawne referencje w Host).

#### ✅ P2. [NAPRAWIONE] Stripe webhook bez weryfikacji podpisu (podatność)
`src/WorkBase.Host/Endpoints/BrandingBillingEndpoints.cs` (linia ~114–119): endpoint `POST /api/billing/webhook` jest `AllowAnonymous`, a treść to zaślepka zwracająca `{ Received = true }` bez weryfikacji nagłówka `Stripe-Signature`.
- **Ryzyko:** atakujący może wstrzykiwać fałszywe zdarzenia billingowe (anulowanie subskrypcji, fałszywe faktury, zmiana statusu tenanta) po wdrożeniu prawdziwej logiki na tym endpoincie.
- **Naprawa:** weryfikacja podpisu przez `Stripe.net` (`EventUtility.ConstructEvent(json, sig, webhookSecret)`) zanim jakakolwiek logika ruszy; sekret z konfiguracji.

#### ✅ P3. [NAPRAWIONE] Dashboard Hangfire z tymczasową autoryzacją
`src/WorkBase.Host/Program.cs` (linia 107): `// TODO: Replace with proper auth filter after T-E02` + `HangfireLocalRequestOnlyFilter`. Na serwerze produkcyjnym za proxy filtr „localhost only" bywa nieskuteczny → ekspozycja jobów.
- **Naprawa:** filtr sprawdzający rolę `workbase-admin` z tokenu Keycloak.

### 🟠 WYSOKIE

#### P4. Frontend: 101+ inline styles zamiast Tailwind/design-systemu
README obiecuje Tailwind + Shadcn/ui, a faktycznie style są inline (`style={{…}}`) z powtarzanymi hex-ami (`#111827`, `#6b7280`, `#e5e7eb`). Brak spójnej warstwy tematu mimo istnienia `frontend/src/theme/tokens.ts`.
- **Skutek:** trudna utrzymywalność, brak spójności wizualnej, brak łatwego white-labelu (a to feature premium!).
- **Naprawa:** migracja do Tailwind lub CSS Modules + konsekwentne użycie tokenów z `theme/`.

#### ✅ P5. [NAPRAWIONE] Rozjazd dokumentacja ↔ rzeczywistość
- README: struktura `workbase-web/` → w repo jest `frontend/`.
- README: „Zustand, Tailwind, Shadcn/ui" → nieobecne/nieużyte.
- Docs sugerują wczesny etap (0% logiki) → faktycznie 15 modułów zaimplementowanych.
- **Naprawa:** zaktualizowano [README.md](../README.md) — usunięto nieużywane biblioteki ze stacku, poprawiono nazwę folderu (`frontend/`), dodano brakujące 5 modułów do listy architektury, poprawiono nazwy plików docker/npm scripts (`type-check`).

#### P6. Frontend bez testów
Pipeline CI uruchamia `npm test` (Vitest), ale brak plików testowych na froncie. Backend ma testy (unit/integration/architecture), front — zero.
- **Naprawa:** testy dla krytycznych ścieżek (auth guard, hooki API, komponenty z logiką jak `TaskListPage`).

#### ✅ P7. [NAPRAWIONE] `FormBuilderPage` — anti-pattern z efektem w `useState`
`frontend/src/pages/forms/FormBuilderPage.tsx` (~L50–55): efekt uboczny wewnątrz inicjalizatora `useState` zamiast `useEffect`.
- **Naprawa:** przeniesiono do `useEffect` z poprawną listą zależności `[selectedDef, loadDef]`.

### 🟡 ŚREDNIE

- **✅ P8. [NAPRAWIONE] Brak Error Boundaries** na froncie — awaria zapytania nie ma graceful fallbacku (np. `EmployeeCardPage`, gdy `id` = undefined). **Naprawa:** dodano [`ErrorBoundary`](../frontend/src/components/ui/ErrorBoundary.tsx) (class component) opakowujący routing na dwóch poziomach — globalnym (auth/provider) i per-trasa (reset po zmianie `location.pathname`), więc awaria jednej podstrony nie wyłącza całej aplikacji.
- **P9. „State explosion"** w `TaskListPage.tsx` (40+ `useState`) — kandydat na `useReducer`/kontekst. *(nie tknięte — wymaga większego refaktoru, patrz plan)*
- **P10. Walidacja FluentValidation tylko w 2 modułach** (Organization, Leave). Pozostałe 13 walidują inline lub wcale — niespójność i ryzyko braku walidacji na wejściu. *(nie tknięte)*
- **P11. Magic strings zamiast enumów** — statusy jako stringi (`"Active"`, `"ClockIn"`, `"Completed"`) w encjach TimeTracking/Workflow. *(nie tknięte)*
- **P12. Luki w testach backendu** — brak testów: Stripe webhook, API key auth, walidatorów, egzekwowania data scope. *(nie tknięte — nowy webhook z P2 nadal bez testu)*
- **P13. Endpointy `AllowAnonymous`** poza webhookiem: `RegisterTenant`, `GetOnboardingStatus` (self-service — zamierzone, ale warte rate-limitingu przeciw nadużyciom rejestracji). *(nie tknięte)*
- **✅ P14. [ZWERYFIKOWANE/NAPRAWIONE] Nieużyte abstrakcje na froncie** — korekta wcześniejszych ustaleń:
  - `QrScanner` — **false positive audytu wstępnego**: komponent JEST używany w [`KioskPage.tsx`](../frontend/src/pages/KioskPage.tsx) (2 miejsca). Nic nie zmieniono.
  - `StorageAdapter` (`shared/storage.ts`) — potwierdzone: używany tylko w `pushNotifications.ts` dla klucza VAPID. Pozostawiony (abstrakcja tania, może się przydać przy Tauri/Capacitor). Nic nie zmieniono.
  - `NotificationAdapter` (`shared/notification.ts`) — potwierdzone jako **martwy kod** (0 realnych konsumentów; realna logika powiadomień web-push żyje osobno w `services/pushNotifications.ts` i bezpośrednio używa `Notification`/`PushManager`). **Naprawa:** plik usunięty, eksport z `shared/index.ts` usunięty.

### 🆕 Nowe ustalenia z etapu remediacji

- **P15. [NAPRAWIONE] Podatność wysokiego ryzyka w `react-router-dom` 7.13.2`** — `npm audit` wykrył krytyczne/wysokie CVE w vendored `turbo-stream` (nieautoryzowane RCE przez deserializację), open redirect (`//`-prefixed redirects), DoS przez `__manifest` i CSRF przez PUT/PATCH/DELETE. **Naprawa:** `npm audit fix` podniósł rozwiązaną wersję do `react-router-dom@7.18.1` (zakres `^7.13.2` w `package.json` bez zmian, `package-lock.json` zaktualizowany). Zweryfikowano `type-check` + `build` — bez regresji.
- **P16. Pozostała podatność umiarkowana: `uuid` (przez `exceljs`)** — wymaga downgrade `exceljs` do `3.4.0` (breaking change), więc **nie naprawiono automatycznie** — decyzja pozostawiona właścicielom projektu (funkcja eksportu do Excel może się zmienić).
- **P17. Bundle frontend >500 kB po minifikacji** (`dist/assets/index-*.js` ~1.75 MB / 483 kB gzip) — Vite ostrzega o braku code-splittingu. Rekomendacja: `React.lazy()` dla tras administracyjnych/rzadziej używanych + `manualChunks` w `vite.config.ts`. *(nie tknięte — wymaga decyzji o podziale tras)*

---

## 4. Co jest dobre / przydatne (zachować)

### Backend — mocne strony
- ✅ **Spójna architektura DDD** w 15 modułach: identyczne wzorce nazewnicze (Command/Handler/Validator/Repository/Event/Dto/Endpoints).
- ✅ **Result pattern** + centralny `GlobalExceptionHandler` (ProblemDetails RFC 9110, `errorCode`).
- ✅ **Multi-tenancy egzekwowana realnie** — globalny filtr EF Core (`WorkBaseDbContext`), `tenant_id` z claima JWT, filtr uprawnień blokuje brak tenanta. Pokryte testami integracyjnymi (`TenantIsolationTests`, `PermissionEnforcementTests`).
- ✅ **Brak SQL injection** — EF Core + Dapper wyłącznie parametryzowany.
- ✅ **CORS whitelist-based**, konfigurowalny, `AllowCredentials` tylko dla dozwolonych origin.
- ✅ **Public API** z kluczami hashowanymi SHA256, wygasaniem, IP allowlist, scope'ami.
- ✅ **Testy architektoniczne** (NetArchTest) pilnują granic modułów i izolacji warstwy Domain.
- ✅ **Brak `NotImplementedException`, brak zakomentowanego kodu, brak sekretów w kodzie.**
- ✅ **Directory.Build.props**: .NET 9, nullable enable, warnings-as-errors — surowa kompilacja.

### Frontend — mocne strony
- ✅ **TypeScript strict** (pełny), `noUncheckedIndexedAccess`, brak `any`, brak `console.*`.
- ✅ **Warstwa API spójna** — fetch wrapper z `ApiError`, TanStack Query z konsekwentnymi `queryKey` i inwalidacją cache.
- ✅ **Bezpieczne tokeny** — OIDC state w `sessionStorage`, tokeny w pamięci (nie localStorage), brak `dangerouslySetInnerHTML` (brak XSS).
- ✅ **PWA** (service worker, cache strategy), i18n (PL), guards uprawnień (`PermissionGate`, `FeatureGate`).
- ✅ Backend proxowany przez nginx (brak bezpośrednich wywołań, brak problemów CORS w prod).

### Infrastruktura — mocne strony
- ✅ **Docker Compose** kompletny (Postgres 16, Keycloak 24, MinIO, Seq, API).
- ✅ **CI/CD dojrzałe** — 3 workflowy: CI (build/test/lint/migracje/OpenAPI), migrations (manual dispatch), CD do GHCR + Render.
- ✅ **Keycloak realm** predefiniowany (role, klienci PKCE, brute-force protection, krótki access token 5 min).
- ✅ **render.yaml** + Neon (serverless PG) — blueprint wdrożeniowy gotowy.
- ✅ **Skrypty ops** (`tools/`): backup/restore DB, setup staging, deploy.

---

## 5. Potencjał rozwojowy i biznesowy

### Atuty biznesowe
- **Szeroki, spójny zakres funkcjonalny** już w kodzie (15 modułów) — wartość znacznie wyższa niż typowe MVP.
- **Konfiguracja zamiast kodu** (workflowy JSON, custom fields JSONB, definiowalne role/statusy) — realny fundament pod adaptację per-klient bez deployu.
- **Feature-flag licensing** wpisany w architekturę → gotowy mechanizm monetyzacji tierów.
- **Multi-tenant od podstaw** → skalowanie na wielu klientów bez re-architektury.
- **Modular monolith** → możliwość wydzielenia modułów do usług w przyszłości bez przepisywania.

### Szanse (quick wins → wartość)
1. **Wpiąć 5 gotowych modułów** (P1) — natychmiast odblokowuje Sales (CRM), AI, Cases, Forms, Contacts jako funkcje premium. To gotowy, opłacony już kod.
2. **Dokończyć billing (Stripe)** — realny mechanizm przychodu; obecnie zaślepka.
3. **White-label** — dokończyć branding + warstwę tematu (P4) → argument sprzedażowy dla większych klientów.
4. **Frontend dla modułów premium** (Sales/Cases/AI) — backend gotowy, brakuje UI.
5. **Desktop (Tauri)** — kanał dystrybucji dla klientów enterprise (dziś szkielet).

### Ryzyka rozwojowe
- **Silnik workflow** jest sercem Leave/Tasks/akceptacji — złożoność wysoka; regresje tu kaskadują.
- **Data scope filtering** — błąd = wyciek danych między zespołami/tenantami; wymaga twardych testów.
- **Anomaly detection** (zmiany nocne, DST) — łatwo o edge-case'y.
- **Rozjazd docs↔kod** obniża onboarding nowych deweloperów i zaufanie do repo.

---

## 6. Priorytetyzowany plan działań

| # | Działanie | Priorytet | Nakład | Efekt | Status |
|---|---|---|---|---|---|
| 1 | Wpiąć AI/Cases/Contacts/Forms/Sales do `ModuleDiscovery` (lub auto-discovery) | 🔴 Krytyczny | Niski | Odblokowanie gotowego kodu + funkcji premium | ✅ Wykonane |
| 2 | Weryfikacja podpisu Stripe webhook | 🔴 Krytyczny | Niski | Zamknięcie podatności billing | ✅ Wykonane |
| 3 | Docelowa autoryzacja dashboardu Hangfire (rola admin) | 🔴 Wysoki | Niski | Bezpieczeństwo ops | ✅ Wykonane |
| 4 | Zsynchronizować README/docs ze stanem faktycznym | 🟠 Wysoki | Niski | Onboarding, zaufanie | ✅ Wykonane (README) |
| 5 | Migracja inline styles → Tailwind/tokeny + white-label | 🟠 Wysoki | Średni | Utrzymywalność, sprzedaż | ⏳ Nierozpoczęte |
| 6 | Fix `FormBuilderPage` (useEffect) + Error Boundaries | 🟠 Wysoki | Niski | Stabilność UI | ✅ Wykonane |
| 7 | Testy frontendu (Vitest) dla krytycznych ścieżek | 🟠 Wysoki | Średni | Regresje | ✅ Wykonane (api client + ErrorBoundary) |
| 8 | Rozszerzyć FluentValidation na wszystkie moduły | 🟡 Średni | Średni | Spójność, walidacja wejścia | ⏳ Nierozpoczęte |
| 9 | Enumy zamiast magic strings (statusy) | 🟡 Średni | Średni | Typosafety | ⏳ Nierozpoczęte |
| 10 | Testy: webhook, API key auth, data scope, walidatory | 🟡 Średni | Średni | Pokrycie bezpieczeństwa | ✅ Częściowo (webhook — 4 testy; API key/data scope pozostają) |
| 11 | Refactor `TaskListPage` (useReducer), reużywalna paginacja | 🟢 Niski | Średni | DX | ⏳ Nierozpoczęte |
| 12 | Usunąć/wykorzystać nieużyte abstrakcje (StorageAdapter, NotificationAdapter, QrScanner) | 🟢 Niski | Niski | Czystość kodu | ✅ Zweryfikowane (patrz P14) |
| 13 | Naprawić podatność `react-router-dom` (npm audit) | 🔴 Krytyczny | Niski | Zamknięcie RCE/CSRF/open-redirect | ✅ Wykonane |
| 14 | Naprawić podatność `uuid` (przez `exceljs`, wymaga breaking downgrade) | 🟡 Średni | Niski | Domknięcie ostatniej luki npm audit | ⏳ Nierozpoczęte (decyzja biznesowa) |
| 15 | Code-splitting bundla frontendu (`React.lazy`, `manualChunks`) | 🟢 Niski | Średni | Wydajność ładowania | ✅ Wykonane |

---

## 7. Podsumowanie (scorecard)

| Obszar | Ocena | Komentarz |
|---|---|---|
| Spójność architektury (backend) | 9/10 | Wzorcowa jednolitość 15 modułów |
| Spójność docs ↔ kod | 4/10 | Istotne rozjazdy (ścieżki, biblioteki, status) |
| Jakość kodu backend | 8/10 | Czysto; braki: walidacja niespójna, magic strings |
| Jakość kodu frontend | 6/10 | Dobra baza; inline styles i brak testów ciążą |
| Bezpieczeństwo | 7/10 | Solidny fundament; 3 konkretne dziury do domknięcia |
| Martwy kod | 5/10 | 5 osieroconych modułów + drobne nieużyte abstrakcje |
| Funkcjonalność | 9/10 | Bardzo szeroki, realnie zaimplementowany zakres |
| Potencjał biznesowy | 9/10 | Multi-tenant + feature flags + premium moduły gotowe |
| Testy | 6/10 | Backend OK, frontend zero, luki w bezpieczeństwie |
| Infrastruktura/CI-CD | 9/10 | Dojrzałe, gotowe do wdrożeń |

**Werdykt:** Dojrzały technicznie, spójny modular monolith z bardzo szerokim, realnie zaimplementowanym zakresem funkcjonalnym i mocnym fundamentem pod SaaS. Wszystkie 3 problemy krytyczne oraz podatność w `react-router-dom` zostały **usunięte w ramach tego audytu**. Pozostałe punkty o wysokim/średnim priorytecie (inline styles, FluentValidation, magic strings, dodatkowe testy bezpieczeństwa) są opisane w sekcji 6 i czekają na realizację.

---

## 8. Log wykonanych poprawek

Wszystkie poniższe zmiany zweryfikowano: `dotnet build` (0 błędów/ostrzeżeń), `dotnet test tests/WorkBase.Tests.Architecture` (5/5 pass), `dotnet test tests/WorkBase.Tests.Integration` (17/17 pass, powtórzone 2× dla stabilności), `npm run type-check`, `npm run lint` (0 błędów), `npm test` (10/10 pass), `npm run build` (sukces).

| Poprawka | Pliki zmienione | Weryfikacja |
|---|---|---|
| **P1** — wpięcie 5 modułów | [ModuleDiscovery.cs](../src/WorkBase.Infrastructure/ModuleDiscovery.cs) | Build + testy architektury |
| **P2** — Stripe webhook | [BrandingBillingEndpoints.cs](../src/WorkBase.Host/Endpoints/BrandingBillingEndpoints.cs), `WorkBase.Host.csproj` (+Stripe.net), `appsettings.json`, `.env.example`, `docker-compose.dev.yml`, `docker-compose.staging.yml`, `staging.env.example` | Build + [WebhookSecurityTests.cs](../tests/WorkBase.Tests.Integration/WebhookSecurityTests.cs) |
| **P3** — Hangfire auth | [HangfireAdminAuthorizationFilter.cs](../src/WorkBase.Infrastructure/BackgroundJobs/HangfireAdminAuthorizationFilter.cs) (nowy), [Program.cs](../src/WorkBase.Host/Program.cs) | Build |
| **P5** — README sync | [README.md](../README.md) | Manualna weryfikacja treści |
| **P7** — FormBuilderPage fix | [FormBuilderPage.tsx](../frontend/src/pages/forms/FormBuilderPage.tsx) | type-check |
| **P10 (część)** — testy webhooka | [WebhookTestFactory.cs](../tests/WorkBase.Tests.Integration/WebhookTestFactory.cs) (nowy), [WebhookSecurityTests.cs](../tests/WorkBase.Tests.Integration/WebhookSecurityTests.cs) (nowy) — 4 testy: fail-closed bez sekretu, odrzucenie sfałszowanego podpisu, akceptacja poprawnego podpisu, brak wymogu autoryzacji. Przy okazji naprawiono `EventUtility.ConstructEvent(..., throwOnApiVersionMismatch: false)` w [BrandingBillingEndpoints.cs](../src/WorkBase.Host/Endpoints/BrandingBillingEndpoints.cs) (bez tego każdy webhook o innej wersji API Stripe niż wersja skompilowana w Stripe.net kończył się fałszywym „nieprawidłowy podpis") oraz poprawiono usuwanie hosted services w [WorkBaseWebFactory.cs](../tests/WorkBase.Tests.Integration/WorkBaseWebFactory.cs) (Hangfire rejestruje się przez factory delegate, więc filtr po `ImplementationType` go nie usuwał — testy wisiały ~60s na dispose) | `dotnet test` 17/17, 2× powtórzone |
| **P15** — code-splitting frontendu | [App.tsx](../frontend/src/App.tsx) (wszystkie strony przez `React.lazy` + `Suspense`), [TeamAttendancePage.tsx](../frontend/src/pages/time/TeamAttendancePage.tsx) (dynamiczny `import('exceljs')` zamiast statycznego) | Główny bundle: 1755 kB → 476 kB (gzip 483→144 kB); `exceljs` (940 kB) i pozostałe strony ładowane on-demand |
| **P7 (frontend)** — testy Vitest | [vite.config.ts](../frontend/vite.config.ts) (blok `test`), [src/test/setup.ts](../frontend/src/test/setup.ts) (nowy), [client.test.ts](../frontend/src/api/client.test.ts) (nowy, 6 testów), [ErrorBoundary.test.tsx](../frontend/src/components/ui/ErrorBoundary.test.tsx) (nowy, 4 testy) + `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `jsdom` jako devDependencies | `npm test` 10/10 |
| **P8** — Error Boundary | [ErrorBoundary.tsx](../frontend/src/components/ui/ErrorBoundary.tsx) (nowy), `components/ui/index.ts`, [App.tsx](../frontend/src/App.tsx) | type-check, build, testy komponentu |
| **P14** — martwy kod | usunięto `frontend/src/shared/notification.ts` + eksport z `shared/index.ts`; `QrScanner`/`StorageAdapter` zweryfikowane jako używane (bez zmian) | type-check, build |
| **P15 (npm audit)** — `react-router-dom` CVE | `frontend/package-lock.json` (7.13.2 → 7.18.1 przez `npm audit fix`) | type-check, build |

**Nie wykonane w tej turze** (wymagają większego nakładu lub decyzji biznesowej): P5 (migracja stylów na Tailwind), P8 (FluentValidation we wszystkich modułach), P9 (enumy), P10 pozostała część (testy API key auth, data scope), P11 (rate limiting rejestracji / refactor TaskListPage), P14 (breaking downgrade `exceljs` dla `uuid` CVE).
