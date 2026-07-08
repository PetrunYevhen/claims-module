# Claims Module — FNOL Intake & Reserve Management

Vertical slice of an insurance Claims Management System: First Notice of Loss (FNOL) intake and Reserve Management, built as a personal project. Full scope implementation (not a trimmed MVP) — greenfield, no external system dependencies beyond what's provisioned below. Built primarily by Claude Code, with the workflow and corrections documented in [`AI-WORKFLOW.md`](AI-WORKFLOW.md).

> **Status**: Backend and frontend complete and E2E-verified with a real multi-role auth flow. Runs locally as a one-command Docker Compose stack, and is also deployed live on Azure (App Service + Azure SQL + Blob Storage + Static Web App) with GitHub Actions CI/CD on every push to `main`. See [`ARCHITECTURE.md`](ARCHITECTURE.md#azure-architecture-deployed) for the deployed design.

## Live Deployment

- **App**: https://victorious-moss-0356ff210.7.azurestaticapps.net
- **API**: https://webapp-claims-module-api.azurewebsites.net/swagger

Both run on Azure free-tier resources (App Service F1, SQL Database free-limit tier). First request after a period of inactivity can take 10-30 seconds — the SQL Database auto-pauses on the free tier and the App Service F1 worker cold-starts; this is expected, not a bug.

## Prerequisites

- Docker Desktop (Apple Silicon: uses `azure-sql-edge`, the ARM64-compatible SQL Server image)
- .NET SDK 9.0+ (only needed if running the API outside Docker)
- Node.js 20+ / Angular CLI 22 (only needed if running the frontend outside Docker)

## Quick Start — one command

```bash
docker compose up -d --build
```

Brings up three containers: `claims-module-db` (SQL Server), `claims-module-api` (ASP.NET Core, applies EF Core migrations automatically on startup — no manual `dotnet ef database update` needed), `claims-module-web` (Angular production build served by nginx, which reverse-proxies `/api` and `/uploads` to the API container so the browser only ever talks to one origin).

- **App**: http://localhost:4200
- **Swagger**: http://localhost:5080/swagger
- **Hangfire dashboard**: http://localhost:5080/hangfire
- **Health check**: http://localhost:5080/api/health

Stop with `docker compose down` (add `-v` to also drop the database volume and uploaded documents).

## Test Credentials

There is no real identity provider — three hardcoded mock accounts simulate the three FRS roles. Pick one from the login screen (also fetched live from `GET /api/auth/users`), or call `POST /api/auth/login` directly:

| Username | Password | Role | Can approve reserves up to |
|---|---|---|---|
| `handler@claims.local` | `password123` | handler | auto-approved only (≤ $10,000) |
| `supervisor@claims.local` | `password123` | supervisor | $100,000 |
| `manager@claims.local` | `password123` | manager | unlimited, incl. $10M aggregate override |

Tokens are short-lived JWTs (HMAC-signed, 8h expiry) issued by the mock login endpoint — see `Auth/JwtTokenGenerator.cs`.

## Feature Walkthrough

1. **Log New Claim** (FNOL intake, 3-step wizard) — search a seeded policy (5 policies pre-loaded, one deliberately expired to exercise the out-of-force badge), enter loss details, add parties/risk objects, optionally set an initial reserve, review, submit. A `ClaimNumber` like `CLM-2026-0000001` is generated atomically per calendar year.
2. **Claims Dashboard** — paginated/filterable list of all claims with status, cause of loss, and total reserves.
3. **Claim Detail**, five tabs:
   - **Overview** — loss details, severity, editable notes, status-transition button (only valid next statuses per the state machine are offered).
   - **Parties** — add/remove parties; a claim must always keep at least one Claimant.
   - **Reserves** — per-component summary cards, full transaction history, add/approve/reject/retract a reserve. Approval authority is gated by role and amount (see thresholds above); reserves above $10,000 stay `PendingApproval` until a sufficiently senior role approves them, which then fires a simulated GL-posting background job.
   - **Documents** — upload/download files against the claim (local filesystem in dev/Docker, swappable to Azure Blob Storage — see Configuration below).
   - **Audit Log** — full append-only history of everything that happened to the claim, newest first.
4. **Role switcher** — the toolbar lets you swap the active mock user without re-navigating, useful for demonstrating the approval workflow (submit as handler, approve as supervisor).

## Manual Development Setup (without Docker for app code)

Useful for active backend/frontend development with hot reload; the database still runs in Docker either way.

1. **Start the database only**:
   ```bash
   docker compose up -d db
   ```
2. **Run the API** (auto-migrates on startup):
   ```bash
   cd backend
   dotnet run --project src/ClaimsModule.API
   ```
   API listens on http://localhost:5080.
3. **Run the frontend**:
   ```bash
   cd frontend/claims-ui
   npm install
   npm start
   ```
   Dev server on http://localhost:4200, configured (`environment.development.ts`) to call the API directly at `http://localhost:5080/api` — CORS is enabled on the API for this origin by default.

## Configuration

| Key | Location | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.json` / `ConnectionStrings__DefaultConnection` env var | SQL Server connection (app DB + Hangfire storage share one instance) |
| `Storage:Provider` | `appsettings.json` | `LocalFileSystem` (default) or `AzureBlob` |
| `ConnectionStrings:AzureBlobStorage` | required only when `Storage:Provider=AzureBlob` | Azure Storage connection string |
| `Jwt:Key` / `Issuer` / `Audience` / `ExpiryMinutes` | `appsettings.json` | Mock JWT signing config |
| `Cors:AllowedOrigins` | `appsettings.json` | Origins allowed to call the API directly (only relevant when the frontend isn't served through the nginx proxy) |
| `SA_PASSWORD` | shell env, read by `docker-compose.yml` | Overrides the default `YourStrong!Passw0rd` SQL Server sa password |

Default local DB credentials are `sa` / `YourStrong!Passw0rd`.

## Solution Structure

```
claims-module/
├── backend/
│   ├── ClaimsModule.sln
│   ├── Dockerfile
│   └── src/
│       ├── ClaimsModule.Domain          # entities, enums, state-machine/authority rules — no dependencies
│       ├── ClaimsModule.Application     # MediatR commands/queries, FluentValidation, DTOs, AutoMapper
│       ├── ClaimsModule.Infrastructure  # Hangfire job definitions, Azure Blob / local file storage
│       ├── ClaimsModule.Persistence     # EF Core DbContext, entity configs, migrations
│       └── ClaimsModule.API             # controllers, middleware, mock auth, DI wiring
├── frontend/claims-ui/                  # Angular 22 app (standalone components, signals)
│   ├── Dockerfile
│   ├── nginx.conf
│   └── src/app/
│       ├── core/                        # models, HTTP services, interceptors
│       ├── features/                    # claims-list, fnol-intake, claim-detail (+5 tabs)
│       └── shared/                      # dialogs
├── docs/ (see ARCHITECTURE.md, AI-WORKFLOW.md — kept at repo root instead)
├── .github/workflows/                   # CI/CD: deploy-api.yml, deploy-web.yml
└── docker-compose.yml                   # db + api + web, one-command local stack
```

Clean Architecture dependency direction: `API → Application, Infrastructure, Persistence` / `Persistence, Infrastructure → Application → Domain`. Full detail (CQRS flow, business rules, background jobs, auth design) in [`ARCHITECTURE.md`](ARCHITECTURE.md).

## Database

10 business entities per the FRS: `Claim` (aggregate root), `LossEvent`, `ClaimParty`, `ClaimRiskObject`, `ClaimReserveComponent`, `ReserveHistory` (append-only reserve ledger), `ClaimDocument`, `ClaimAuditLog` (hard append-only — updates/deletes throw), `CauseOfLossCode` and `Policy` (seeded reference data) — plus `ClaimNumberCounter` for atomic per-year claim number generation.

Conventions: GUID PKs (`NEWSEQUENTIALID()`), `decimal(19,4)` money, `datetimeoffset(7)` UTC timestamps, soft delete on business tables, full audit columns, single seeded tenant (`OrganisationId`), `RowVer` optimistic concurrency on the two aggregate roots (`Claim`, `ClaimReserveComponent`).

## Known Limitations / Deliberately Out of Scope

- No "assigned handler" search/assignment mechanism — no command sets `AssignedHandlerId`, so the Dashboard doesn't filter by it.
- No manual retry endpoint for a `Failed` GL-posting job — the badge is display-only; Hangfire's own automatic retries (5 attempts, exponential backoff) are the only recovery path.
- `Severity` is computed/stored but never settable from the UI.
- Mock JWT auth only — no real identity provider, no refresh tokens, no password hashing (deliberate per FRS 11.4, this is a simulation).

## Azure Deployment

Live on Azure — App Service (API), Azure SQL Database (free tier), Blob Storage, Static Web App (frontend). Full resource list, CI/CD pipeline, and design rationale in [`ARCHITECTURE.md`](ARCHITECTURE.md#azure-architecture-deployed).
