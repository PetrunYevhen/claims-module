# Architecture

## Overview

Clean Architecture, five backend projects plus one Angular frontend, wired for CQRS via MediatR. The domain layer has zero external dependencies; every other layer points inward toward it.

```
                    ┌────────────────────┐
                    │  ClaimsModule.API   │  controllers, middleware, mock JWT auth, DI composition root
                    └─────────┬───────────┘
              ┌────────────────┼────────────────┐
┌─────────────▼──────┐ ┌───────▼────────┐ ┌──────▼──────────────┐
│ ClaimsModule.       │ │ ClaimsModule.  │ │ ClaimsModule.        │
│ Persistence          │ │ Infrastructure │ │ Application          │
│ (EF Core DbContext,  │ │ (Hangfire jobs,│ │ (MediatR commands/   │
│  migrations, entity  │ │  Blob/local    │ │  queries, validators,│
│  configs)             │ │  storage)      │ │  DTOs, AutoMapper)   │
└─────────┬────────────┘ └───────┬────────┘ └──────┬────────────┘
          └───────────────────────┴──────────────────┘
                                   │
                         ┌─────────▼──────────┐
                         │ ClaimsModule.Domain │  entities, enums, state-machine + authority rules
                         └─────────────────────┘
```

`Application` depends only on `Domain`. `Persistence` and `Infrastructure` depend on `Application` (to implement its interfaces, e.g. `IClaimsDbContext`, `IStorageService`) and `Domain`. `API` composes all four. This means the entire business-rule surface (state transitions, reserve authority thresholds, validation) is testable without a database, a queue, or HTTP.

## Domain Model

Aggregate root is `Claim`; everything else hangs off it by `ClaimId`.

| Entity | Role |
|---|---|
| `Claim` | ClaimNumber, PolicyId?, ClientName, Status, Severity, Notes, `AggregateLimitOverride` flag, RowVer |
| `LossEvent` | 1:1 with Claim — date, description, location, cause-of-loss code, estimated amount |
| `ClaimParty` | role (Claimant/Witness/ThirdParty/...), person or company fields |
| `ClaimRiskObject` | asset type/description/damage, IsPrimary |
| `ClaimReserveComponent` | one row per (Claim, Component) pair — Indemnity/Expense/ALAE/SubrogationRecoverable — holds `CurrentAmount`, RowVer |
| `ReserveHistory` | append-only ledger of every reserve transaction against a component (see below) |
| `ClaimDocument` | uploaded file metadata + blob path |
| `ClaimAuditLog` | hard append-only event log — the only entity where `SaveChangesAsync` throws on Modified/Deleted |
| `CauseOfLossCode`, `Policy` | seeded reference data (10 codes, 5 policies incl. one expired) |
| `ClaimNumberCounter` | one row per calendar year, incremented atomically for claim-number generation |

Conventions applied uniformly: GUID PKs with `NEWSEQUENTIALID()`, `decimal(19,4)` money, `datetimeoffset(7)` UTC timestamps, global soft-delete query filter (reflection-based, applied to every entity implementing the soft-delete marker except `ClaimAuditLog`), full audit columns (`CreatedAt/UpdatedAt/UserCreated/UserModified`), single seeded `OrganisationId` (this is a single-tenant simulation, not a real multi-tenant system), `RowVer` optimistic concurrency on `Claim` and `ClaimReserveComponent` only — the two rows that see concurrent writes.

## Business Rules

### Claim status state machine

Enforced centrally in `Domain/Rules/ClaimStatusTransitionRules.cs`, consulted by both the `TransitionClaimStatusCommand` validator and the frontend (to only offer valid next statuses in the UI):

```
Draft → Open
Open → UnderInvestigation | PendingPayment | Closed | Withdrawn
UnderInvestigation → Open | PendingPayment | Closed | Withdrawn
PendingPayment → Closed
Closed → Reopened
Reopened → Open
Withdrawn → (terminal)
```

### Reserve authority thresholds (BR-R-02)

`Domain/Rules/ReserveAuthorityRules.cs`, evaluated on `|amount|`:

| Amount | Initial status | Who can approve |
|---|---|---|
| ≤ $10,000 | `AutoApproved` immediately | n/a — never enters the approval queue |
| $10,000 – $100,000 | `PendingApproval` | supervisor or manager |
| > $100,000 | `PendingApproval` | manager only |

A $10,000,000 aggregate cap per claim (sum of all Approved/AutoApproved reserve amounts) is checked at submission time in `CreateReserveCommandHandler` — if breached without `Claim.AggregateLimitOverride` set (a manager-only override, `SetAggregateLimitOverrideCommand`), a `VALIDATION_ISSUE_ADDED` audit entry is written; the actual block happens at approval time via the same rule.

### Reserve ledger design

`ReserveHistory` rows are never deleted or have their financial fields mutated after creation — new transactions append new rows, and `ClaimReserveComponent.CurrentAmount` is a running balance updated only when a transaction becomes Approved/AutoApproved. This is event-sourcing-flavored but not literal full immutability: a `PendingApproval` row *does* transition in place (`ApprovalStatus`, `ApprovedByUserId`/`ApprovedAt` or `RejectedByUserId`/`RejectedAt` get set on the same row) rather than being superseded by a new row — a deliberate simplification over a fully event-sourced ledger, since the FRS asks for an auditable history, not strict append-only-with-compensating-entries semantics (that stricter rule is reserved for `ClaimAuditLog`).

### BR-A-01 — hard append-only audit log

`ClaimAuditLog` is the one entity where immutability is enforced at the database-write level, not just by convention: `ClaimsDbContext.SaveChangesAsync` is overridden to throw if any tracked `ClaimAuditLog` entry has `EntityState.Modified` or `EntityState.Deleted` before delegating to the base implementation.

### BR-C-06 — reserves require a linked policy

`CreateReserveCommandHandler` throws a `BusinessRuleException` if `Claim.PolicyId` is null. The frontend enforces this earlier (hides the reserve section during FNOL intake when no policy is selected) purely for UX — the backend check is authoritative.

### BR-C-04 — atomic claim number generation

`ClaimNumberGenerator` (Persistence layer) issues numbers like `CLM-2026-0000001` via a raw SQL `UPDATE ClaimNumberCounters SET LastSequence = LastSequence + 1 OUTPUT INSERTED.LastSequence WHERE Year = @year` (inserting the year row first if absent) inside the same transaction as claim creation — a table-based counter rather than a SQL `SEQUENCE`, because the format resets per calendar year and a plain sequence object doesn't reset itself.

## CQRS Flow

Every write and non-trivial read is a MediatR command/query. Example — `POST /api/claims`:

```
ClaimsController.Create(CreateClaimRequest)
  → CreateClaimCommand
  → MediatR pipeline: ValidationBehavior<CreateClaimCommand, ClaimDetailDto>
      runs every registered FluentValidation validator for the request;
      throws Application.Common.Exceptions.ValidationException on failure
      (aliased against FluentValidation.ValidationException to avoid ambiguity)
  → CreateClaimCommandHandler
      - generates ClaimNumber via IClaimNumberGenerator
      - persists Claim + LossEvent + ClaimParty[] + ClaimRiskObject[] in one SaveChangesAsync
      - writes a CLAIM_CREATED entry via IAuditLogService
  → AutoMapper (ProjectTo where reading, Map where returning a just-written entity) → ClaimDetailDto
  → ExceptionHandlingMiddleware translates any Application-layer exception into the FRS 10.4 shape:
      { type, title, status, errors, correlationId }
      ValidationException/BusinessRuleException → 422, NotFoundException → 404, else → 500 (logged)
```

`ICorrelationIdProvider` (backed by `CorrelationIdMiddleware`, which reads/generates an `X-Correlation-Id` header) threads a correlation ID through every error response and every audit log entry for that request.

## Background Jobs (Hangfire, SQL Server storage)

### `PostGLReserveChangeJob` — fire-and-forget, enqueued on approval

Enqueued via `IBackgroundJobEnqueuer` immediately after a reserve transaction becomes `AutoApproved` or is manually `Approved`. Simulates posting a GL journal entry (DR Change in Outstanding Reserves / CR Outstanding Loss Reserves) as an audit log entry. Idempotent by design: it re-checks `ReserveHistory.PostingStatus` and no-ops if already `Posted`, which matters because Hangfire can re-deliver a job after a crash between execution and acknowledgment. Retries automatically (`[AutomaticRetry(Attempts = 5, DelaysInSeconds = [5, 15, 30, 60, 120])]`); on the final attempt, instead of letting the exception propagate (which would just mark the Hangfire job Failed and stop), the handler catches it, sets `PostingStatus = Failed`, and writes a `GL_POSTING_FAILED` audit entry so the failure is visible in-app.

### `SlaMonitoringJob` — recurring, every 15 minutes

Registered via `RecurringJob.AddOrUpdate` at API startup with cron `*/15 * * * *`. Scans `Draft`/`Open` claims not updated in 48+ hours and writes a `SLA_BREACH_DETECTED` audit entry — throttled to at most one breach entry per claim per 24 hours by checking the most recent matching audit entry first. Never changes claim status or sends notifications — purely observational, per FRS 12.2.

## Storage Abstraction

`IStorageService` (`UploadAsync`, `GetDownloadUrlAsync`) has two implementations selected by `Storage:Provider` at startup:

- `LocalFileSystemStorageService` (default, used in Docker/dev) — writes under `App_Data/uploads/{organisationId}/{claimId}/`, served back by ASP.NET Core static file middleware mounted at `/uploads`. No TTL/SAS semantics — `GetDownloadUrlAsync` just returns the relative static-file path.
- `AzureBlobStorageService` — writes to a real Azure Storage container and returns a time-limited SAS URL.

The frontend's `resolveUrl()` helper treats any URL starting with `http` as already-absolute (the Azure case) and otherwise prefixes it with the API's origin (the local-filesystem case) — see `documents-tab.ts`.

## Auth Design (mock, FRS 11.4)

Three hardcoded accounts (`Auth/MockUser.cs`) map 1:1 to the three FRS roles (handler/supervisor/manager). `POST /api/auth/login` checks username+password against the hardcoded list and issues an HMAC-SHA256-signed JWT (`JwtTokenGenerator`) carrying `NameIdentifier`, `Role`, and a custom `org` claim. Authorization uses a global `FallbackPolicy` requiring an authenticated user on every endpoint by default; individual actions opt out with `[AllowAnonymous]` (login, mock-user list, health check).

`ClaimsPrincipalCurrentUserService` (`ICurrentUserService`) reads `OrganisationId`/`UserId`/`Role` off the current `ClaimsPrincipal` — but Hangfire jobs execute outside any HTTP request, so `HttpContext` is null there. The service falls back to a well-known "system" identity (`SystemUserId = 00000000-0000-0000-0000-000000000001`, `Role = "system"`, `OrganisationId = SeedConstants.OrganisationId`) whenever `HttpContext` is unavailable, rather than throwing — this was a real bug caught during Stage 7 testing (see `AI-WORKFLOW.md`).

## Frontend Architecture

Angular 22, standalone components throughout, signals for local component state, the new `@if`/`@for` control-flow syntax, and the signal-based `input()`/`output()` component API — no `NgModules` anywhere.

- **`core/services/*`** — one typed HTTP service per resource (`ClaimsService`, `ReservesService`, `ReferenceDataService`, `AuthService`); components never call `HttpClient` directly.
- **`core/interceptors/*`** — functional interceptors: `auth.interceptor.ts` attaches the Bearer token, `error.interceptor.ts` shows a snackbar on any HTTP error and force-logs-out on 401.
- **`app.routes.ts`** — all three route groups (`claims`, `claims/new`, `claims/:id`) are lazy-loaded via `loadComponent`.
- **`features/fnol-intake`** — the largest single component: a 3-step `mat-stepper` (policy + loss details → parties/risk objects → initial reserve + review) with reactive forms and a policy-typeahead autocomplete.
- **`features/claim-detail`** — a header (status badge + transition dropdown) plus 5 lazily-rendered tabs, each its own component.

## Local Docker Compose Topology

```
┌──────────────┐   /api, /uploads   ┌──────────────┐        ┌──────────────┐
│  web (nginx)  │ ──────proxy──────▶ │  api (.NET)   │ ──TDS──▶│  db (SQL Srv) │
│  :4200 → :80  │                    │  :5080 → :8080│        │  :1433        │
└──────────────┘                    └──────────────┘        └──────────────┘
```

- `web` is a two-stage build: `node:24-alpine` runs `ng build --configuration production` (output is a pure static SPA — no SSR), then `nginx:alpine` serves `dist/claims-ui/browser` and reverse-proxies `/api/*` and `/uploads/*` to the `api` container by Docker Compose service name. Because the browser only ever sees one origin, no CORS configuration is needed in this path (CORS is still configured on the API for the `ng serve`-on-4200-against-API-on-5080 case used in manual dev).
- `api` is a two-stage build: `mcr.microsoft.com/dotnet/sdk:9.0` restores/publishes, `mcr.microsoft.com/dotnet/aspnet:9.0` runs it. On startup it calls `ClaimsDbContext.Database.Migrate()` inside a DI scope before the host starts serving — the whole stack comes up from a cold, empty SQL Server container with a single `docker compose up`, no manual `dotnet ef database update` step.
- `db` is `mcr.microsoft.com/azure-sql-edge` — the ARM64-compatible SQL Server image (Apple Silicon has no native `mssql/server` image). It ships without `sqlcmd`, so the healthcheck is a raw TCP probe (`bash -c '</dev/tcp/localhost/1433'`) instead of the usual `sqlcmd`-based one.
- Two named volumes: `claims-module-db-data` (SQL Server data files) and `claims-module-uploads` (mounted at `/app/App_Data/uploads` in the `api` container) so both the database and locally-stored documents survive `docker compose down` (not `-v`).

## Target Azure Architecture (Stage 9, planned)

Not yet provisioned; documented here so the deployment story is explicit even though it hasn't been executed.

| Concern | Local (current) | Azure (planned) |
|---|---|---|
| API | `api` container | Azure App Service for Containers (or Container Apps), same Dockerfile |
| Frontend | `web` container (nginx) | Azure Static Web App (serving the same `ng build` output) or a second App Service |
| Database | `azure-sql-edge` container | Azure SQL Database |
| Document storage | Local filesystem volume | Azure Blob Storage (`AzureBlobStorageService` already implemented, just needs `Storage:Provider=AzureBlob` + a real connection string) |
| Secrets | Plaintext env vars / `appsettings.json` | Azure Key Vault, referenced from App Service configuration |
| CI/CD | none | GitHub Actions: build+test on PR, build+push container images and deploy on merge to `main` |

The Static Web App / second-App-Service split (rather than reusing the nginx-proxy pattern) matters for Azure specifically because Azure Static Web Apps have built-in support for proxying `/api/*` to a linked backend without needing to own and maintain an nginx config.

## Key Decisions & Trade-offs

- **EF Core pinned to 9.0.15** — the "latest" tag resolved to a 10.x preview incompatible with `net9.0`; pinning avoided a forced early upgrade to .NET 10.
- **Swashbuckle.AspNetCore pinned to 7.2.0** — 10.x pulls in Microsoft.OpenApi 2.x, which moved types out of the `Microsoft.OpenApi.Models` namespace the codebase (and most tutorials) still target.
- **Mock JWT auth instead of a real IdP** — explicitly scoped this way by the FRS (11.4); avoids the complexity of Entra ID / Auth0 integration for a project whose actual subject is the Claims domain, not identity.
- **Single seeded tenant** — `OrganisationId` is a constant (`SeedConstants.OrganisationId`), not a real multi-tenant boundary; simplifies the whole system without weakening the demonstrated business logic, since the FRS doesn't require multi-tenancy.
- **`azure-sql-edge` for local dev** — the only SQL Server-compatible image with a native ARM64 build, required for Apple Silicon without emulation overhead.
