# Contributing — WorkBase

## Zanim zaczniesz

1. Przeczytaj [README.md](README.md) — stack, architektura, setup.
2. Zapoznaj się z [03 — Architektura techniczna](docs/03-technical-architecture.md) — struktura modułów, konwencje kodu, wzorce.
3. Sprawdź [04 — Backlog](docs/04-detailed-backlog.md) — task nad którym pracujesz ma ID (np. `T-E05-002`).

## Workflow pracy

### 1. Branch

```bash
git checkout develop
git pull origin develop
git checkout -b feature/E05-employee-crud
```

Nazwa brancha: `<typ>/<epik>-<krótki-opis>`
- `feature/E08-clock-in-out`
- `bugfix/E10-leave-balance-calc`
- `hotfix/auth-token-refresh`

### 2. Commitowanie

Format: `<type>(<scope>): <description>`

```bash
git commit -m "feat(org): add employee CRUD endpoints"
git commit -m "fix(time): handle midnight clock-out edge case"
git commit -m "test(workflow): add approval flow integration tests"
```

**Typy:** `feat`, `fix`, `chore`, `test`, `docs`, `refactor`, `style`, `ci`, `perf`

**Scope = moduł:** `org`, `identity`, `time`, `leave`, `tasks`, `workflow`, `dashboard`, `notif`, `docs`, `infra`, `docker`, `ci`

### 3. Pull Request

- PR do brancha `develop`
- Tytuł: `[E05] Employee CRUD — domain + API + tests`
- Opis: co zmieniono, jaki task z backlogu, jak przetestować
- Wymagany minimum 1 code review
- CI musi przejść (build + test + lint)

## Konwencje kodu

### Backend (.NET / C#)

| Reguła | Przykład |
|---|---|
| Nazwy klas | `PascalCase` → `EmployeeAssignment` |
| Nazwy metod | `PascalCase` → `GetEmployeeById` |
| Zmienne prywatne | `_camelCase` → `_employeeRepository` |
| Interfejsy | `I` prefix → `IEmployeeRepository` |
| Namespace | `WorkBase.Modules.Organization.Domain.Entities` |
| Async suffix | `GetEmployeeByIdAsync` |
| Tabele DB | `snake_case` z prefixem modułu → `org_employees` |
| Kolumny DB | `snake_case` → `employee_id`, `created_at` |

### Frontend (React / TypeScript)

| Reguła | Przykład |
|---|---|
| Komponenty | `PascalCase` → `EmployeeForm.tsx` |
| Hooks | `camelCase` z `use` → `useEmployees.ts` |
| API files | `camelCase` → `org.api.ts` |
| Store | `camelCase` → `authStore.ts` |
| Stałe | `UPPER_SNAKE_CASE` → `MAX_FILE_SIZE` |
| CSS | Tailwind utility classes (bez customowych CSS) |

### Struktura modułu (.NET)

Każdy moduł ma 4 warstwy:

```
WorkBase.Modules.{Nazwa}/
├── Domain/           # Entities, ValueObjects, Events, Specifications
├── Application/      # Commands, Queries, EventHandlers, Validators
├── Infrastructure/   # Persistence (DbContext, Configs, Migrations, Repos), Services
└── Api/              # Controllers, Requests, Responses, {Module}Module.cs
```

**Zasady modularności:**
- Moduł A **nie importuje** typów z modułu B bezpośrednio
- Komunikacja między modułami: `WorkBase.Contracts` (interfejsy, DTOs) + Domain Events
- Test: `WorkBase.Tests.Architecture.ModuleBoundaryTests` weryfikuje granice

### Struktura modułu (frontend)

```
modules/{nazwa}/
├── pages/            # Pełne strony (route targets)
├── components/       # Komponenty modułu
└── hooks/            # Hooki modułu
```

## Testy

### Backend

| Typ | Projekt | Co testujemy |
|---|---|---|
| Unit | `WorkBase.Tests.Unit` | Domain logic, validators, commands/queries (mocked dependencies) |
| Integration | `WorkBase.Tests.Integration` | API endpoints z prawdziwą DB (Testcontainers PostgreSQL) |
| Architecture | `WorkBase.Tests.Architecture` | Granice modułów (ArchUnit) |

```bash
# Wszystkie testy
dotnet test

# Tylko unit
dotnet test --filter "FullyQualifiedName~Tests.Unit"

# Tylko integracyjne
dotnet test --filter "FullyQualifiedName~Tests.Integration"
```

### Frontend

```bash
cd workbase-web
npm run test          # Vitest
npm run lint          # ESLint
npm run typecheck     # tsc --noEmit
```

## Checklist przed PR

- [ ] Kod kompiluje się bez błędów (`dotnet build` / `npm run build`)
- [ ] Testy przechodzą (`dotnet test` / `npm run test`)
- [ ] Lint przechodzi (`npm run lint`)
- [ ] TypeScript przechodzi (`npm run typecheck`)
- [ ] Nowe migracje EF Core wygenerowane (jeśli zmiana schematu)
- [ ] OpenAPI spec aktualna (endpoints mają atrybuty `[ProducesResponseType]`)
- [ ] Brak hardkodowanych wartości tenant-specific (użyj `cfg_tenant_configs`)
- [ ] Sensitive data nie logowane (hasła, tokeny)
- [ ] `[Authorize]` / `[RequirePermission]` na nowych endpointach

## Pytania?

Sprawdź dokumentację w `docs/`. Jeśli coś jest niejasne — pytaj w PR review.
