# AGENTS.md

## Layout and entrypoints

- No root `package.json` and no `.sln`. `backend/` (.NET 9) and `frontend/` (Angular 22) are independent; run commands from inside each directory.
- Backend wiring is all in `backend/Program.cs` (DI, seeding, CORS, DB provider selection, `/` and `/api/health`). Controllers are thin; logic lives in `backend/Services/`.
- `backend/Data/DbSeeder.cs` runs on every startup and creates the admin user plus seed listings.
- Frontend uses lazy standalone routes in `frontend/src/app/app.routes.ts`. All HTTP goes through `frontend/src/app/core/api.ts`; the JWT interceptor reads `localStorage['reai_token']`.

## Commands

- Backend: `cd backend && dotnet run` → `http://localhost:5000`. Build check: `dotnet build backend/RealEstateAI.Api.csproj`.
- Frontend: `cd frontend && npm ci && npm start` → `http://localhost:4200`. Production check: `npm run build` (**not** plain `ng build` — the script also regenerates `_redirects`).
- No test suite exists despite `npm test` and `tsconfig.spec.json`: `angular.json` has no `test` target and there are no spec files. Verify with builds plus a browser smoke test.
- Demo admin: `admin@realestateai.pk` / `Admin@123`.

## Runtime quirks

- No EF migrations. `DbSeeder` uses `EnsureCreated`; after a schema change, delete local `realestateai.db` and restart (there is a narrow auto-reset only when a table probe throws). Hosted Postgres is created fresh.
- SQLite locally. PostgreSQL is selected when the connection string contains `Host=` or `Server=`. `Program.cs` converts Render's `postgres://` URL and defaults the port to 5432 when absent — do not remove that fallback; it previously caused `Invalid port: -1` and a startup crash.
- Blank `Ai:ApiKey` means a deterministic local agent with no external calls; a configured key falls back to local on failure. OCR uses `tesseract.js` in the browser, so keep it in `allowedCommonJsDependencies` in `angular.json`.
- `Cors:AllowedOrigins` is a comma-separated string in config/env (not an array), read from `Cors__AllowedOrigins` in the environment.
- Uploads go to `backend/wwwroot/uploads` and are ephemeral on Render (no persistent disk).

## Deployment

- Render must be created as a **Blueprint** from `render.yaml`, not via New → Web Service. Service `realestateai-api` → `https://realestateai-api-p1nb.onrender.com` (the plain subdomain is taken globally); health check `/api/health`.
- `netlify.toml` publishes `dist/real-estate-ai/browser` — Angular 22 nests output under `browser/`, not the parent folder.
- The live frontend is a public Netlify agent-runner site (`https://polite-paletas-4014d8.netlify.app`) deployed by ZIP upload, not connected to Git. That flow ignores `_redirects`, so `core/api.ts` uses the absolute Render origin in production. Any new frontend origin must be added to Render `Cors__AllowedOrigins` or API calls fail.
- Render free tier sleeps after ~15 minutes idle; the first request can take 30–60 seconds.

## Coding workflow

1. Check `git status` first; treat uncommitted work as the user's and don't overwrite it. Read the relevant service/controller or component before editing.
2. Keep backend logic in `backend/Services/` (controllers stay thin) and frontend HTTP in `frontend/src/app/core/` services; don't scatter fetch calls in components.
3. Verify in this order: `dotnet build backend/RealEstateAI.Api.csproj` → `cd frontend && npm run build` → browser smoke test of the affected page/endpoint. Builds are the only automated check available.
4. Schema/entity change: delete local `realestateai.db`, restart, and confirm `DbSeeder` still probes the new table. New frontend origin: add it to Render `Cors__AllowedOrigins`.
5. Commit with a scoped message and push `main`. Backend changes need **Manual Deploy → Deploy latest commit** on Render (auto-deploy is off); frontend changes need a rebuilt ZIP uploaded as a new Netlify version.
6. Never commit secrets; use environment variables and keep dev defaults only in `appsettings.json`.
