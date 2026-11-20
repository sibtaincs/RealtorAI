# AGENTS.md

## Layout and entrypoints

- No root `package.json` and no `.sln`. `backend/` (.NET 9) and `frontend/` (Angular 22) are independent; run commands from inside each directory.
- Backend wiring is all in `backend/Program.cs` (DI, seeding, CORS, DB provider selection, `/` and `/api/health`). Controllers are thin; logic lives in `backend/Services/`.
- `backend/Data/DbSeeder.cs` runs on every startup and creates the admin user plus seed listings. Local SQL Server schema is versioned by EF migrations in `backend/Data/Migrations`.
- Frontend uses lazy standalone routes in `frontend/src/app/app.routes.ts`. All HTTP goes through `frontend/src/app/core/api.ts`; the JWT interceptor reads `localStorage['reai_token']`.
- `tests/RealEstateAI.Api.Tests/` is an xUnit integration project using `WebApplicationFactory<Program>`; `Program.cs` ends with `public partial class Program;` for that reason.

## Commands

- Backend: `cd backend && dotnet run` → `http://localhost:5000`. Build check: `dotnet build backend/RealEstateAI.Api.csproj`.
- New migration: `dotnet ef migrations add <Name> --project backend --startup-project backend --context AppDbContext --output-dir Data/Migrations` from repo root. **Rebuild after adding one** — EF generates the files after its build, so a stale DLL reports "No migrations were found in assembly".
- Frontend: `cd frontend && npm ci && npm start` → `http://localhost:4200`. Production check: `npm run build` (**not** plain `ng build` — the script also regenerates `_redirects`).
- Backend tests: `dotnet test tests/RealEstateAI.Api.Tests/RealEstateAI.Api.Tests.csproj` — boots the real API in-process and needs SQLEXPRESS running; tests are read-only. Frontend has **no** test suite (`npm test`/`tsconfig.spec.json` exist but `angular.json` has no `test` target and there are no specs) — verify with a build plus browser smoke test.
- Demo admin: `admin@realestateai.pk` / `Admin@123`.

## Runtime quirks

- Local DB is SQL Server Express at `localhost\SQLEXPRESS` (Windows auth), database `RealEstateAI`; SQLite is only a fallback connection-string option.
- SQL Server uses `Database.MigrateAsync()` in `DbSeeder`; hosted PostgreSQL still uses `EnsureCreated` (fresh DB per deploy). Migrations are generated for SQL Server — do not apply them to Postgres.
- `Program.cs` picks the provider by connection string: `Host=` → Npgsql, `Server=` → SQL Server, otherwise SQLite. It converts Render's `postgres://` URL and defaults the port to 5432 when absent — do not remove that fallback; it previously caused `Invalid port: -1` and a startup crash.
- Blank `Ai:ApiKey` means a deterministic local agent with no external calls; a configured key falls back to local on failure. OCR uses `tesseract.js` in the browser, so keep it in `allowedCommonJsDependencies` in `angular.json`.
- `Cors:AllowedOrigins` is a comma-separated string in config/env (not an array), read from `Cors__AllowedOrigins` in the environment.
- Uploads go to `backend/wwwroot/uploads` and are ephemeral on Render (no persistent disk).

## Deployment

- `.github/workflows/ci.yml` runs backend build + the 10 integration tests against a SQL Server 2022 service container on every push/PR, builds the frontend, and uploads the site as the `frontend-site` artifact. Deploy jobs only run on `main`.
- CI needs repo secrets `RENDER_DEPLOY_HOOK`, `NETLIFY_AUTH_TOKEN`, `NETLIFY_SITE_ID` (optional `CI_SA_PASSWORD`). Without them, CI passes and deploy steps warn + skip. The Netlify site is an agent-runner project, so if `netlify-cli deploy` cannot target it, upload the `frontend-site` artifact as a new version manually.
- Render must be created as a **Blueprint** from `render.yaml`, not via New → Web Service. Service `realestateai-api` → `https://realestateai-api-p1nb.onrender.com` (the plain subdomain is taken globally); health check `/api/health`.
- `netlify.toml` publishes `dist/real-estate-ai/browser` — Angular 22 nests output under `browser/`, not the parent folder.
- The live frontend is a public Netlify agent-runner site (`https://polite-paletas-4014d8.netlify.app`) deployed by ZIP upload, not connected to Git. That flow ignores `_redirects`, so `core/api.ts` uses the absolute Render origin in production. Any new frontend origin must be added to Render `Cors__AllowedOrigins` or API calls fail.
- Render free tier sleeps after ~15 minutes idle; the first request can take 30–60 seconds.

## Coding workflow

1. Check `git status` first; treat uncommitted work as the user's and don't overwrite it. Read the relevant service/controller or component before editing.
2. Keep backend logic in `backend/Services/` (controllers stay thin) and frontend HTTP in `frontend/src/app/core/` services; don't scatter fetch calls in components.
3. Verify in this order: `dotnet build backend/RealEstateAI.Api.csproj` → `dotnet test tests/RealEstateAI.Api.Tests/RealEstateAI.Api.Tests.csproj` → `cd frontend && npm run build` (if frontend changed) → browser smoke test of the affected page/endpoint.
4. Schema/entity change: add an EF migration, rebuild, restart, and confirm `DbSeeder` still seeds. New frontend origin: add it to Render `Cors__AllowedOrigins`.
5. Commit with a scoped message and push `main`; CI gates deploys on green builds/tests. If deploy secrets are not configured, fall back to **Manual Deploy → Deploy latest commit** on Render and a rebuilt ZIP upload as a new Netlify version.
6. Never commit secrets; use environment variables and keep dev defaults only in `appsettings.json`.
