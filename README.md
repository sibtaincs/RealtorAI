# RealEstateAI 🇵🇰🏠

An AI-powered real estate agent platform for the Pakistani property market, built with **Angular 22** and **ASP.NET Core 9**.

## Features

### Core MVP
- **Property listings** — 14 seeded properties across Karachi, Lahore, Islamabad & Rawalpindi
- **Search & filters** — city, type, sale/rent, bedrooms, text search, pagination
- **AI agent chat** — answers in Roman Urdu/English, recommends matching listings
- **AI price estimator** — compares similar listings to estimate fair value
- **AI listing descriptions** — generate descriptions from property details
- **Lead management** — public inquiry form + admin inbox with status updates
- **Auth** — JWT-based register/login with `Admin` and `Agent` roles
- **Map view** — OpenStreetMap embed on property detail pages (no API key needed)

### Advanced features
- **Photo uploads** — authenticated multi-image upload to `backend/wwwroot/uploads` (jpg/png/webp/gif, max 5 MB each), served at `/uploads/{fileName}`
- **WhatsApp lead follow-ups** — one-tap `wa.me` deep links with pre-filled Roman Urdu messages, plus follow-up history. Optional Meta Cloud API sending when `WhatsApp:PhoneNumberId` and `WhatsApp:AccessToken` are configured.
- **Document verification checklist** — Pakistan-specific items (Registry/Fard, Mutation, NOC, CNIC copies, agreement, allotment letter, etc.) auto-created for every property; agents can tick items and track progress.
- **Saved searches & alerts** — save any filter combination, toggle alerts, run a check manually, and receive in-app notifications when new matching properties arrive. A background worker checks enabled alerts periodically.
- **Notifications bell** — unread badge in the header; clicking an alert opens the saved search results with filters applied.

### Phase 1 — AI that does the work
- **AI Listing Copilot** — on the Post Property page, type a few lines (e.g. "3 bed house, 5 marla, DHA Lahore, 2.2 crore") or upload photos first; AI fills the entire form: title, description, type, city, locality, bedrooms, bathrooms, area and price. Uses a vision-capable model when `Ai:ApiKey` is set, otherwise a built-in Roman Urdu extractor.
- **Sell-in-X-days Price Advisor** — choose 15 / 30 / 60 days and get a recommended price, range, confidence level and the comparable listings it was based on. One click applies the price to the form.
- **Listing Quality Score** — a 100-point checklist (title, description, photos, realistic price, location, key facts, verified documents) with per-item tips so sellers know exactly how to improve a listing.
- **Lead scoring** — every inquiry gets a 0–100 score and a Hot / Warm / Cold label with reasons (valid phone, buying intent, property-specific, recency). The Leads inbox can sort by hot leads so agents call the best prospects first.
- **AI WhatsApp replies** — the "🤖 AI Reply" button on a lead generates a Roman Urdu reply using the available listings, then saves it to the follow-up history with a ready `wa.me` link (or sends directly via Meta Cloud API when configured).

> When `Ai:ApiKey` is configured with a vision model (e.g. `gpt-4o-mini`), the Listing Copilot reads uploaded photos for extra signals. Without a key everything runs locally with the rule-based agent — no external calls.

### Phase 2 — Verification & trust
- **AI document scan (OCR)** — on the property detail page, agents upload a photo of a Registry deed, CNIC, NOC, agreement, etc. Tesseract.js reads the text **in the browser** (no server/API key needed) and the backend auto-verifies every matching checklist item. A manual "paste document text" fallback is available when OCR cannot load.
- **Verified listing badges** — every property API response now includes `verifiedDocuments`, `totalDocuments` and `isTrusted` (≥60% of documents verified, minimum 3). Property cards show `✅ Verified` or `📄 x/y`; detail pages show a badge plus document count.
- **Duplicate & fraud check** — the Post Property page compares a draft listing against live listings by title, locality, type and area, flags suspicious prices against comparable per-marla medians, and returns a Low / Medium / High risk score with reasons. No AI key required.
- **AI Market Check (buyer-side)** — property detail pages show a 0–100 risk report with comparable median price, document-verification status, price flags and a plain-Roman-Urdu verdict.
- **Voice search** — a 🎤 button in the hero uses the browser's Web Speech API (`ur-PK`) to turn speech into a property search. Works in Chrome/Edge; other browsers show a friendly hint.

## Run it locally

### 1. Backend (API) — port 5000

```bash
cd backend
dotnet run
```

The SQLite database (`realestateai.db`) is created and seeded automatically on first run. If you upgrade from an older schema, delete `realestateai.db` once and restart.

### 2. Frontend (Angular) — port 4200

```bash
cd frontend
npm start
```

Open **http://localhost:4200**. The Angular app detects localhost and talks to `http://localhost:5000/api`.

### Demo admin login

```
Email:    admin@realestateai.pk
Password: Admin@123
```

## Configuration

All settings live in `backend/appsettings.json` and can be overridden with environment variables.

### Real AI (optional)

The AI features work out of the box with a built-in rule-based Roman Urdu agent. To use a real LLM:

```json
"Ai": {
  "ApiKey": "sk-...",
  "Endpoint": "https://api.openai.com/v1/chat/completions",
  "Model": "gpt-4o-mini"
}
```

Any OpenAI-compatible endpoint works. If the call fails, the service falls back to the local agent.

### WhatsApp Cloud API (optional)

Without these, follow-ups open a `wa.me` link with a pre-filled message. To send via Meta's API instead:

```json
"WhatsApp": {
  "PhoneNumberId": "your-phone-number-id",
  "AccessToken": "your-meta-access-token"
}
```

### Database & CORS

- Local: SQLite (`realestateai.db`).
- Hosted: set `ConnectionStrings__DefaultConnection` to a PostgreSQL connection string; the app switches to Npgsql automatically.
- `Cors__AllowedOrigins` should contain your frontend origin(s), comma-separated.

## Deploy for free (Render + Netlify)

The source is on GitHub at `https://github.com/sibtaincs/RealtorAI.git` (branch `main`).

### 1. Deploy the API on Render

1. In Render, choose **New → Blueprint** and select the `RealtorAI` repo. It reads `render.yaml`.
2. This creates:
   - `realestateai-api` — free Docker web service (port 8080), health check at `/api/health`
   - `realestateai-db` — free PostgreSQL database (connection string injected automatically)
3. Wait for the first deploy to finish, then note the API URL, e.g. `https://realestateai-api.onrender.com`.
4. In the service's **Environment** tab, set `Cors__AllowedOrigins` to your **real** Netlify URL
   (comma-separated if you want both `*.netlify.app` aliases). The blueprint ships with two likely defaults.
5. Optionally set `Ai__ApiKey`, `WhatsApp__AccessToken`, `WhatsApp__PhoneNumberId`.
   `Jwt__Key` is generated automatically by the blueprint — no need to set it.
6. Confirm the API is alive: `GET https://realestateai-api.onrender.com/api/health` → `{"status":"ok",...}`.

> Render's free plan **sleeps after ~15 minutes of inactivity**; the first request after sleep can take ~30–60 seconds.

### 2. Deploy the Angular app on Netlify

1. **Add new site → Import from Git** and select the same `RealtorAI` repo.
2. Netlify reads `netlify.toml`:
   - Base: `frontend`
   - Build: `npm ci && npm run build`
   - Publish: `dist/real-estate-ai`
   - `API_ORIGIN` (set to your Render API URL)
3. **Important:** in **Site configuration → Environment variables**, set
   `API_ORIGIN = https://YOUR-ACTUAL-RENDER-URL.onrender.com` (no trailing slash), then trigger a deploy.
   The build script `frontend/scripts/write-redirects.mjs` uses it to generate `dist/_redirects`, which
   proxies `/api/*` and `/uploads/*` to the API and falls back to `index.html` for SPA routes.
4. Your public URL is the Netlify site URL (e.g. `https://realestateai.netlify.app`).
   Make sure that exact URL is listed in `Cors__AllowedOrigins` on Render (step 1.4), then re-deploy Render if you changed it.

### Local verification before deploying

```bash
# backend
dotnet build backend/RealEstateAI.Api.csproj
# frontend
cd frontend && npm ci && npm run build
# check generated redirects
type dist\real-estate-ai\_redirects
```

> **Note:** Render's free web service has no persistent disk. Uploaded images under `wwwroot/uploads` disappear on restart/redeploy. For permanent photos, add a paid Render disk or switch `ImageService` to Azure Blob Storage / S3. The free PostgreSQL database is persistent.

## Project structure

```
backend/
  Controllers/   Auth, Properties, Leads, Ai, Uploads, Documents,
                 SavedSearches, Notifications, FollowUps, Trust
  Data/          AppDbContext, DbSeeder
  Dtos/          Request/response records
  Models/        Entities and enums (including SavedSearch, Notification,
                 DocumentChecklistItem, FollowUp)
  Services/      Property, Lead, Ai, Token, Image, Document, Trust,
                 SavedSearch, Notification, WhatsApp, AlertWorker
  Dockerfile     Render container build
frontend/
  public/_redirects   Netlify proxy rules for /api and /uploads
  src/app/
    core/        Models, services, auth interceptor, API/asset URL helpers
    pages/       Home, property detail, login, post property, leads,
                 saved searches
    shared/      Property card, AI chat widget, OCR document checklist
render.yaml      Render API + free PostgreSQL blueprint
netlify.toml     Netlify Angular build config
```

## Future ideas

- Server-side OCR fallback with a vision LLM for low-quality document photos
- Real price-prediction model trained on historical transactions
- Azure Blob Storage for durable image hosting
- Email/SMS follow-ups alongside WhatsApp
- Client-side image resizing before upload
