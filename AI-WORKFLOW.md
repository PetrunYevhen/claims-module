# AI Workflow

This project was built primarily by Claude Code (Sonnet 5), with the user acting as product owner and reviewer — approving scope, confirming each stage's completion, and making the calls that were genuinely his to make (full scope vs. MVP, Apple Silicon target, monorepo layout). This document describes how that collaboration actually worked, not an idealized version of it.

## Tools

- **Claude Code** (CLI agent) — all code, all commits' worth of implementation, all of this documentation.
- **Playwright (chromium)**, driven by Claude Code — real browser E2E verification of the frontend, not just `tsc`/build success. Installed on demand (`playwright install chromium --with-deps`) since no project skill for it existed yet.
- **curl** — E2E verification of every backend stage before the frontend existed at all: create a claim, add a party, submit a reserve, approve it as a different role, check the audit log — through the actual HTTP API, not unit tests against handlers in isolation.
- **Docker / docker compose** — local SQL Server (`azure-sql-edge`, ARM64), and later the full containerized stack.

## Process

The work was planned as 10 sequential stages (foundation → Claims CQRS → Reserve Management → Hangfire jobs → Documents → Audit hardening → Mock auth → Angular frontend → Azure deploy → documentation), written into a plan file before any code was touched. Each stage was implemented in full, verified end-to-end against a running system, and only then reported back with a concise summary of what was built and what broke along the way. The user confirmed each stage explicitly before the next one started — nothing was built two stages ahead of what had been reviewed. This mattered in practice: it's what caught the Stage 7 auth regression (below) before it could compound into Stage 8 frontend code being built against a broken backend.

## Example Prompts

**1. Kickoff — scoping the whole project in one line.**
> "я би хотів його реалізувати, буде як пет проєкт для мене чисто виконаний з AI" (*"I'd like to actually build this, as a personal project done entirely by AI"*)

This single sentence turned a request for a written assessment ("write a spec for an intern") into a request to build the real system. It didn't specify scope, platform, or repo layout — those were resolved via a follow-up clarifying question (full scope vs. MVP / Apple Silicon vs. other / monorepo vs. polyrepo) before any plan was written, rather than guessed.

**2. Stage confirmation — terse, trusts the plan already agreed on.**
> "давай 3 пункт роби" (*"go ahead, do item 3"*)

Every subsequent stage was greenlit this tersely (`"так, продовжуй Stage 4"`, `"роби 8"`). The brevity worked because the plan file already contained the detail — the prompt's job was authorization, not specification. This is also why each stage ended with a written report before asking what's next: terse authorization only works if the thing being authorized was clearly described beforehand.

**3. Late-stage, specific infra request.**
> "у докер compos додай контейнери для фронту і беку би все командою запускалося" (*"add containers for the frontend and backend to docker compose so everything starts with one command"*)

Narrower and more technical than the stage-kickoff prompts — by this point the user was directing specific infrastructure decisions rather than approving whole stages, which the workflow accommodated without needing to re-plan anything.

## Cases Where the AI Got It Wrong

### 1. Background jobs crashed after mock auth was introduced (Stage 7 regression)

`ICurrentUserService` was implemented against `HttpContext.User` — reasonable while every code path ran inside an HTTP request. Once Stage 7 added Hangfire background jobs that touch `ICurrentUserService` (via `IAuditLogService`, called from `PostGLReserveChangeJob`), those jobs run with no `HttpContext` at all, and the service threw `InvalidOperationException` on every job execution. This wasn't caught by a build or a type check — it only surfaced because the Stage 7 verification pass actually ran the GL-posting job end-to-end and read its Hangfire status instead of assuming a 200 from the triggering endpoint meant the whole pipeline worked. Fix: `ClaimsPrincipalCurrentUserService` falls back to a well-known "system" identity when `HttpContext` is null, instead of throwing (see `ARCHITECTURE.md`'s Auth Design section for the actual code shape). The lesson generalized: any service reading ambient HTTP state needs an explicit answer for "what happens when there's no HTTP request," and that only gets found by exercising the non-HTTP code path, not by reading the code.

### 2. A real business rule created a silent dead end in the UI (FNOL intake, Stage 8)

The FNOL form let a user set an initial reserve in Step 3 regardless of whether a policy had been linked in Step 1. The backend correctly rejects this (BR-C-06: no reserve without a linked policy) — but by the time that rejection happened, the *claim itself* had already been created successfully via a separate API call earlier in the same submit flow. The chained RxJS `switchMap` propagated the reserve call's error and stopped, so the user was left staring at a form with a spinner that never resolved, while a real claim with a real claim number already existed in the database, invisible to them. This was only caught by scripting the exact user path a real person would take (search a policy → decide not to set a reserve is fine, but *also* testing the no-policy path) through a live browser, not by testing the two API endpoints independently, which both "worked" in isolation. Fixed three ways: hide the reserve section in the template when no policy is selected, a matching defensive check in the submit handler, and a `catchError` around the reserve sub-call so a failure there can never block navigation to the now-real claim — the user sees a distinct "reserve wasn't set" notice instead of getting stuck.

### 3. Angular Material internals silently overwrote form state

`<mat-option [value]="policy" (onSelectionChange)="selectPolicy(policy)">` bound the whole `Policy` object as the option's value. `MatAutocomplete`'s internal selection-trigger logic *also* writes an option's `[value]` into the bound `FormControl` on selection — racing with the explicit `selectPolicy()` handler and leaving a `Policy` object (not a string) sitting in a `FormControl<string>`. The failure didn't show up on selection; it surfaced several keystrokes later as `TypeError: query.trim is not a function` inside a debounced search pipe, once the user typed again after selecting — a delayed, misleading symptom of an immediate cause. Fixed by binding `[value]="policy.policyNumber"` (a string, matching the control's actual type) instead of the object, plus a `typeof query === 'string'` guard added to the search pipe as defense in depth against the same class of mismatch elsewhere.

### 4. A currency format specifier silently picked up the wrong locale

`{amount:C}` (and its C# equivalent) formats using the *current thread's culture*, not a fixed one — on the machine this ran on, that meant reserve amounts briefly rendered with a UAH symbol instead of USD, despite nothing in the code ever mentioning Ukraine. Nothing about this shows up in a type check or a build; it only shows up by actually looking at rendered output. Replaced with the culture-invariant `:N2` specifier everywhere money is formatted (six call sites across the reserve handlers and the GL-posting job), rather than pinning a culture — the app has no real currency-locale requirement, so the fix removes the ambiguity instead of hardcoding an assumption.

## Why This Mattered

Three of the four cases above (#1, #2, #3) would have passed a type-checker, a unit test written against the same mental model that produced the bug, and even a shallow "does the page load" smoke test. All four were only caught because verification meant *running the actual system as a user or background process would experience it* — hitting real HTTP endpoints in sequence, driving a real browser through a real multi-step form, reading actual rendered currency values — rather than trusting that green builds and passing isolated checks meant the feature worked.
