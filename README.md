# WorkBase

> Operacyjno-zarządcza platforma SaaS B2B do zarządzania firmą: czas pracy, struktury, zadania, procesy, urlopy, dashboard kierowniczy.

---

## Stack technologiczny

| Warstwa | Technologie |
|---|---|
| Backend | .NET 9, ASP.NET Core, C#, EF Core 9, Dapper (read-side) |
| Frontend | React 19, TypeScript, Vite, TanStack Query |
| Baza danych | PostgreSQL 16+ |
| Auth | Keycloak 24+ (OIDC, JWT, RBAC) |
| Storage | MinIO (S3-compatible) |
| Logging | Serilog → Seq |
| Background jobs | Hangfire (PostgreSQL storage) |
| Real-time | SignalR |
| Mobile | Capacitor / PWA |
| CI/CD | GitHub Actions |
| Konteneryzacja | Docker, docker-compose |

## Architektura

- **Modular monolith** — każdy z 15 modułów (Organization, Identity, TimeTracking, Leave, Tasks, Workflow, Dashboard, Notification, Documents, Integration, Cases, Contacts, Forms, Sales, AI) to oddzielny .NET project z Domain/Application/Infrastructure/Api
- **CQRS light** — commands i queries przez MediatR
- **Domain Events** — komunikacja między modułami (in-process, MediatR notifications)
- **Multi-tenancy** — shared DB + `tenant_id`, EF Core global query filter
- **RBAC + Data Scope** — role, uprawnienia, zakres widoczności per rola

## Struktura repozytorium

```
WorkBase/
├── src/
│   ├── WorkBase.Host/                  # ASP.NET Core Web Host
│   ├── WorkBase.Shared/                # Shared Kernel (Entity, Result, MediatR)
│   ├── WorkBase.Contracts/             # Inter-module contracts (DTOs, interfaces)
│   ├── WorkBase.Infrastructure/        # Cross-cutting (persistence, email, auth, jobs)
│   └── Modules/
│       ├── WorkBase.Modules.Organization/
│       ├── WorkBase.Modules.Identity/
│       ├── WorkBase.Modules.TimeTracking/
│       ├── WorkBase.Modules.Leave/
│       ├── WorkBase.Modules.Tasks/
│       ├── WorkBase.Modules.Workflow/
│       ├── WorkBase.Modules.Dashboard/
│       ├── WorkBase.Modules.Notification/
│       ├── WorkBase.Modules.Documents/
│       ├── WorkBase.Modules.Integration/      # premium: adaptery Google/MS/Slack/Teams
│       ├── WorkBase.Modules.Cases/            # premium: case management
│       ├── WorkBase.Modules.Contacts/         # premium: kontakty
│       ├── WorkBase.Modules.Forms/            # premium: dynamic form builder
│       ├── WorkBase.Modules.Sales/            # premium: CRM (lead → offer)
│       └── WorkBase.Modules.AI/               # premium: OpenAI summarize/classify
├── tests/
│   ├── WorkBase.Tests.Unit/
│   ├── WorkBase.Tests.Integration/
│   └── WorkBase.Tests.Architecture/
├── frontend/                            # React frontend (Vite), npm package "workbase-web"
│   └── src/
│       ├── api/           # klient HTTP + hooki TanStack Query + typy DTO
│       ├── auth/          # OIDC/Keycloak
│       ├── components/
│       ├── pages/
│       ├── shared/
│       └── theme/
├── docker/
│   ├── Dockerfile
│   ├── Dockerfile.frontend
│   ├── Dockerfile.keycloak
│   ├── docker-compose.dev.yml
│   ├── docker-compose.staging.yml
│   └── keycloak/workbase-realm.json
└── docs/
```

## Wymagania

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/) (frontend)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (PostgreSQL, Keycloak, MinIO, Seq)

## Szybki start

### 1. Klonowanie

```bash
git clone https://github.com/<org>/workbase.git
cd workbase
```

### 2. Infrastruktura (Docker)

```bash
cp .env.example .env
docker compose -f docker/docker-compose.dev.yml up -d
```

Uruchomi: PostgreSQL (5432), Keycloak (8080), MinIO (9000/9001), Seq (5341).

### 3. Backend

```bash
cd src/WorkBase.Host
dotnet restore
dotnet ef database update
dotnet run
```

API: `https://localhost:5001`
Swagger: `https://localhost:5001/swagger`
Health: `https://localhost:5001/health`

### 4. Frontend

```bash
cd frontend
npm ci
npm run dev
```

App: `http://localhost:5173`

## Zmienne środowiskowe

Skopiuj `.env.example` do `.env` i uzupełnij wartości. Kluczowe zmienne:

| Zmienna | Opis | Domyślna (dev) |
|---|---|---|
| `DATABASE_URL` | Connection string PostgreSQL | `Host=localhost;Port=5432;Database=workbase;Username=workbase;Password=workbase` |
| `KEYCLOAK_URL` | URL instancji Keycloak | `http://localhost:8080` |
| `KEYCLOAK_REALM` | Nazwa realm | `workbase` |
| `MINIO_ENDPOINT` | MinIO endpoint | `localhost:9000` |
| `MINIO_ACCESS_KEY` | MinIO access key | `minioadmin` |
| `MINIO_SECRET_KEY` | MinIO secret key | `minioadmin` |
| `SEQ_URL` | Seq logging URL | `http://localhost:5341` |

## Komendy

| Komenda | Opis |
|---|---|
| `dotnet build` | Build backend |
| `dotnet test` | Testy backend (unit + integration) |
| `npm run dev` | Frontend dev server |
| `npm run build` | Frontend production build |
| `npm run lint` | ESLint |
| `npm run type-check` | TypeScript type check |
| `npm test` | Testy frontendu (Vitest) |

## Branching strategy

Stosujemy **GitFlow Light**:

| Branch | Przeznaczenie |
|---|---|
| `main` | Stabilny kod produkcyjny |
| `develop` | Bieżący development, merge target dla feature branchy |
| `feature/<opis>` | Nowa funkcjonalność (z `develop`, merge do `develop`) |
| `bugfix/<opis>` | Naprawa błędu (z `develop`, merge do `develop`) |
| `release/<wersja>` | Przygotowanie release (z `develop`, merge do `main` + `develop`) |
| `hotfix/<opis>` | Pilna poprawka produkcyjna (z `main`, merge do `main` + `develop`) |

**Konwencja nazw branch:** `feature/E05-employee-crud`, `bugfix/E08-clock-out-midnight`

## Konwencja commitów

Format: `<type>(<scope>): <description>`

```
feat(org): add employee CRUD endpoints
fix(time): handle midnight clock-out edge case
chore(docker): update Keycloak to 24.0.5
test(workflow): add approval flow integration tests
docs(readme): update setup instructions
```

Typy: `feat`, `fix`, `chore`, `test`, `docs`, `refactor`, `style`, `ci`, `perf`

## Dokumentacja projektu

- [01 — Definicja produktu](docs/01-product-foundation.md)
- [02 — Roadmapa MVP](docs/02-mvp-roadmap.md)
- [03 — Architektura techniczna](docs/03-technical-architecture.md)
- [04 — Szczegółowy backlog](docs/04-detailed-backlog.md)

## Licencja

Proprietary — © 2026 WorkBase. All rights reserved.
