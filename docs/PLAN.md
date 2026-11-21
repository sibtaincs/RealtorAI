# Phase 3 Roadmap — Growth & Operations

Status: Draft — recommended defaults. Priorities were not explicitly selected; adjust scope before execution.
Last updated: 2026-11-21

## 1. Context

Shipped so far:

- **MVP** — listings, search/filters, AI chat, price estimator, descriptions, leads, auth, map embed.
- **Phase 1** — listing copilot, sell-in-X-days advisor, quality score, lead scoring, AI WhatsApp replies.
- **Phase 2** — OCR document scan, verified badges, duplicate/fraud check, AI market check, voice search.

Phase 3 moves the product from *feature-complete demo* to *a tool agents use every day*: it focuses on the agent workflow, turning saved searches into conversations, visual discovery, and trust at scale.

Existing building blocks to reuse (verified in code):

| Already present | Where |
|---|---|
| `Latitude` / `Longitude` on listings | `backend/Models/PropertyListing.cs` |
| `IsFeatured`, `AgentId`, `ListingStatus`, `CreatedAt` | `backend/Models/PropertyListing.cs` |
| Lead scoring (`Hot` / `Warm` / `Cold`) | `backend/Services/LeadService.cs` |
| Follow-up history with timestamps | `backend/Models/ExtendedEntities.cs` (`FollowUp`) |
| `SavedSearch` with `AlertEnabled` + `LastAlertAt` | `backend/Models/ExtendedEntities.cs` |
| `AlertWorker` loop (`Alerts:IntervalMinutes`, default 2) | `backend/Services/AlertWorker.cs` |
| In-app `Notification` only — no delivery channels | `backend/Services/NotificationService.cs` |
| WhatsApp Cloud API send + `wa.me` fallback | `backend/Services/WhatsAppService.cs` |
| `ApplicationUser.FullName` / `AgencyName`, roles Admin/Agent | `backend/Models/ApplicationUser.cs` |
| No `/dashboard`, `/map`, `/agent/:id`, or admin routes | `frontend/src/app/app.routes.ts` |

## 2. Goals

1. Give agents a single daily workspace instead of a bare leads list.
2. Deliver saved-search matches outside the app (email / WhatsApp) with user control.
3. Make discovery visual and location-first.
4. Close the trust loop with profiles, reviews and moderation.
5. Improve reach and reliability without adding external service dependencies as hard requirements.

## 3. Scope and milestones

### M1 — Agent dashboard + lead pipeline (P0)

**Backend**
- Add `IDashboardService` / `DashboardService` in `backend/Services/`; keep `DashboardController` thin.
- `GET /api/dashboard/summary`: new leads (7d), leads by status, hot/warm/cold counts, average first-response time (`FollowUp.CreatedAt - Lead.CreatedAt`), active listings, verified listings (reuse `ITrustService`), unread notifications.
- `GET /api/dashboard/leads`: hot-first queue with filters, paging and links to `/leads`.

**Frontend**
- New lazy route `/dashboard` in `app.routes.ts`; `core/dashboard.service.ts` for all HTTP.
- Dashboard page: KPI cards, hot-lead queue, listing performance table; header nav link for Admin/Agent only.

**Verification**: add read-only integration assertions for both endpoints; `dotnet build`, `dotnet test`, `npm run build`, browser smoke test. No schema change.

### M2 — Alert delivery beyond in-app (P0)

**Schema (EF migration required)**
- `NotificationPreference`: `UserId`, `InAppEnabled`, `EmailEnabled`, `WhatsAppEnabled`, `DigestOnly`, `QuietHoursFrom/To`, `UpdatedAt`.
- `NotificationDelivery`: `UserId`, `SavedSearchId`, `PropertyListingId`, `Channel`, `SentAt` — for dedupe and audit.

**Backend**
- Add `INotificationDeliveryService` and an `IEmailSender` with an SMTP implementation plus a **log-only dev fallback** when `Smtp:*` is unconfigured (mirrors the deterministic-AI fallback convention).
- Reuse `IWhatsAppService.SendTextAsync` when Meta credentials exist; when they don't, keep the in-app `wa.me` link instead of pretending to send.
- Extend `SavedSearchService.RunAsync` to fan out to enabled channels after the in-app notification is created; dedupe on `(SavedSearchId, PropertyListingId, Channel)` and respect `LastAlertAt`.
- Extend `AlertWorker` only for scheduling/digest batching — keep business logic in services.

**Frontend**
- Preferences UI in `/saved` (global + per saved search) via a new `core/preferences.service.ts`.

**Verification**: integration test that a new match creates one in-app notification and at most one delivery record per channel using a fake sender (no external calls). Migration + rebuild + restart before testing, per AGENTS.md.

**Risks**: Meta outbound alerts need an approved template — in-app link is the fallback. SMTP must never be required for local startup.

### M3 — Map-based discovery (P1)

**Backend**
- Extend `PropertyQueryDto` with `minLat/maxLat/minLng/maxLng` and optional `centerLat/centerLng/radiusKm`.
- Filter in `PropertyService` using the existing coordinate columns; no coordinate migration needed. Add a composite index migration only if query plans show a need.

**Frontend**
- New lazy route `/map`; Leaflet + OpenStreetMap tiles (no API key, consistent with the current embed).
- Reuse home filters; markers from `/api/properties?bbox=...`; marker click → property card → `/property/:id`. Add `bbox` params to `core/property.service.ts`.

**Verification**: integration test for bbox filtering; browser smoke test on `/map`.

**Risks**: some seeded coordinates may be `0` — validate and fix seed data before launch.

### M4 — Agent profiles, reviews & moderation (P1)

**Schema (EF migration required)**
- `AgentProfile`: `UserId`, `Bio`, `Phone`, `PhotoUrl`, `Verified`, cached `RatingAvg` / `RatingCount`.
- `Review`: `PropertyListingId`, `AuthorId`, `Rating` (1–5), `Comment`, `ModerationStatus`, `CreatedAt`.
- `Report`: `PropertyListingId`, `ReporterId`, `Reason`, `Status`, `CreatedAt`.

**Backend**
- `AgentService`, `ReviewService`, `ModerationService`; public profile/review endpoints and an admin-only report queue.
- New reviews default to `Pending` and are hidden from public read until approved.

**Frontend**
- `/agent/:id` profile page, review form on property detail, "report listing" action, `/admin/reports` moderation page.

**Verification**: read-only integration assertions where possible. **Note:** current tests are required to be read-only (AGENTS.md). Write-path coverage needs either seeded deterministic test entities with cleanup or an explicit decision to relax that rule with transactional rollback. Do not silently mutate the shared dev DB.

**Risks**: review spam — add rate limiting and keep moderation mandatory.

### M5 — PWA + i18n + SEO (P2)

- **PWA**: web manifest, service worker/offline shell, install prompt.
- **i18n**: audit hard-coded strings, extract to translation files, keep Roman Urdu as the default voice established in Phase 1/2.
- **SEO**: prerender property-detail routes, canonical + Open Graph meta, `sitemap.xml`, `robots.txt`, listing JSON-LD.

**Risks**: the live Netlify site is an agent-runner ZIP deployment that ignores `_redirects` (AGENTS.md). Prerender/SSR output and any new origin must be validated against that flow, and new frontend origins added to Render `Cors__AllowedOrigins`.

### M6 — Observability + frontend tests (P2)

- **Backend**: correlation IDs and structured request logging; expose lightweight metrics alongside `/api/health`; keep secrets out of logs.
- **Frontend**: add a real test target (`angular.json` currently has none) with Vitest/Jasmine specs for core services and critical pages; add a CI step.
- Extend `.github/workflows/ci.yml` accordingly.

## 4. Sequencing

```
M1 ──► M2 ──► M4        (agent workflow, then delivery, then trust)
  └────► M3              (independent discovery track)
M5, M6                   (parallel quality/reach track after M1–M2)
```

Each milestone is its own PR: build → tests → frontend build → browser smoke → scoped commit → push → green CI → deploy.

## 5. Definition of done (per AGENTS.md)

- `dotnet build backend/RealEstateAI.Api.csproj`
- `dotnet test tests/RealEstateAI.Api.Tests/RealEstateAI.Api.Tests.csproj`
- `cd frontend && npm run build` when frontend changed
- Browser smoke test of the affected page/endpoint
- Schema changes: EF migration → rebuild → restart → confirm `DbSeeder` still seeds
- New frontend origin: add to Render `Cors__AllowedOrigins`
- Scoped commit, push `main`, green CI before deploy

## 6. Non-goals for Phase 3

- Payments, subscriptions or featured-listing billing.
- Native mobile apps (PWA instead).
- Making a real `Ai:ApiKey` mandatory — the deterministic local agent stays the default.
- Replacing PostgreSQL/Netlify/Render hosting.

## 7. Success metrics

- Agents open `/dashboard` at least once per session; hot-lead response time drops.
- Saved-search alerts produce outbound deliveries (or in-app links) and measurable click-through.
- `/map` sessions result in property-detail opens.
- Public listings show verified + review signals; reported listings have a moderation SLA.
- CI green on every push; frontend test suite exists and runs in CI.
