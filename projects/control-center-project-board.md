# Control Center Project Board (Windows-only)

Status: **ACTIVE** (resumed by Tristan)
Owner: main (orchestrator) + coding-shared + debugging + deep-research

## Control Commands (from Tristan)
- If Tristan says **"control center project pause"** (or equivalent), set status to **PAUSED** and stop active execution/delegation for this project.
- If Tristan says **"control center project resume"** (or equivalent), set status to **ACTIVE** and continue from the next unchecked task.

---

## Epic 0 — Decision Lock
- [x] Confirm primary stack: .NET (WPF shell) + WebView2 + SQLite
- [x] Keep Tauri as documented fallback only
- [x] Freeze architecture boundaries (UI → App → Domain → Infra)
- [x] Freeze security baseline (no plaintext secrets, audited mutations)

## Epic 1 — Contracts First
- [x] Define DTOs for Agents / Config / Cron / Skills / Tasks+Usage / Projects
- [x] Define realtime event envelope + versioning rules
- [x] Define standard error model (code/message/details/retryable/correlationId)
- [x] Write contract compatibility policy for v1

## Epic 2 — Solution Skeleton
- [x] Create modules: App / UI / Application / Domain / Infrastructure / Contracts / Tests
- [x] Add dependency rules and enforce in CI/build
- [x] Wire DI container and environment profiles
- [x] Add baseline logging plumbing

## Epic 3 — Core Platform Services
- [x] Build typed Gateway API client
- [x] Build realtime client (WS primary, SSE fallback)
- [x] Build SQLite cache + event journal
- [x] Build Windows secret storage adapter (Credential Manager/DPAPI)
- [x] Build reconnect/resubscribe + dedupe pipeline

## Epic 4 — UI Shell + Design System
- [x] Build shell (left nav, top bar, status strip)
- [x] Build shared primitives (cards/tables/drawers/toasts/dialogs)
- [x] Standardize loading/empty/error states
- [x] Add keyboard/accessibility baseline

## Epic 5 — Feature Modules
### 5.1 Dashboard
- [ ] System health tiles
- [ ] Needs-attention feed

### 5.2 Agents
- [ ] Agent list/detail/status/model/heartbeat
- [ ] Active session/work visibility

### 5.3 Current Tasks + Usage
- [ ] Active subagents/runs panel
- [ ] Recent runs and state
- [ ] Usage/tokens/cost panel

### 5.4 Cron Jobs
- [ ] Job list/detail
- [ ] Run now / enable-disable / create-edit-delete
- [ ] Safety confirms for destructive actions

### 5.5 Skills
- [ ] Installed/enabled/source/health view
- [ ] Safe refresh controls

### 5.6 Config
- [ ] Read-only config explorer
- [ ] Safe config editor (schema validate + diff)
- [ ] Rollback snapshot/apply flow

### 5.7 Projects
- [ ] Project registry cards
- [ ] Repo branch/latest commit/health
- [ ] Links to related agents/tasks

## Epic 6 — Security Hardening
- [ ] Least-privilege runtime checks
- [ ] Sensitive-action re-auth controls
- [ ] Signed update verification path
- [ ] Full mutation audit trail
- [ ] Log/diagnostic secret redaction

## Epic 7 — Reliability Hardening
- [ ] Idempotency keys for mutating commands
- [ ] Circuit breakers + retry/backoff policies
- [ ] Degraded/offline UX behavior
- [ ] Crash recovery/integrity checks for cache
- [ ] Reconnect reconciliation tests

## Epic 8 — Test Gates
- [ ] Unit tests (domain/app rules)
- [ ] Integration tests (API/realtime/storage/auth)
- [ ] Contract tests (gateway endpoints/events)
- [ ] Fault injection (restart/offline/token-expiry/disk pressure)
- [ ] Security scans + signed artifact validation
- [ ] UI smoke tests for core routes

## Epic 9 — Packaging + Ops Readiness
- [ ] Signed Windows installer artifacts
- [ ] Safe update + rollback flow
- [ ] Operator docs (install/connect/troubleshoot/rollback)
- [ ] Release go/no-go checklist

## Epic 10 — Post-Release Loop
- [ ] Triage queue by reliability/UX/perf/security
- [ ] Feed v2 backlog from validated usage

---

## Execution Rule
When resuming, continue from the **first unchecked task** in the earliest incomplete epic.
