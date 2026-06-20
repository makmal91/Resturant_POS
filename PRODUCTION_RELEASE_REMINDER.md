# AKHSOFT POS — Production Release Reminder

Checklist for building, configuring, and deploying a production release. Use this every time you ship to a client server or your own hosting environment.

For local development setup, see [SETUP_REMINDER.md](SETUP_REMINDER.md).

---

## Prerequisites (Build Machine)

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ |
| [Node.js](https://nodejs.org/) | 18+ (LTS recommended) |
| Git | Latest stable |

**Production server** additionally needs SQL Server and a web host (IIS, Nginx, or Kestrel as a Windows/Linux service).

---

## 1. Pre-Release Checklist

- [ ] Release branch/tag checked out (e.g. `main` or release tag)
- [ ] All migrations committed under `Infrastructure/Migrations/`
- [ ] No dev secrets in the build (no `PrivateKeyPem`, no dev JWT keys in committed files)
- [ ] Customer license generated on the **admin/license machine** (not on the POS server)
- [ ] Production connection string, JWT key, and license keys prepared (secrets vault or server config — not Git)
- [ ] Production frontend URL(s) known for CORS
- [ ] Database backup plan in place (if upgrading an existing install)

```powershell
git pull
git status   # working tree clean before release build
```

---

## 2. Git — Production Secrets (Never in Repo)

| Item | Where it lives |
|------|----------------|
| `API/licenses/system.lic` | Customer server only — deliver via secure channel |
| `PrivateKeyPem` | License generator admin machine **only** |
| Production `AesKeyBase64`, `PublicKeyPem` | Server config / environment variables |
| Production `Jwt:Key` | Server config / environment variables |
| SQL connection string (with password) | Server config / environment variables |
| `appsettings.Development.json` | **Do not deploy** to production |

**Deploy from Git:** application binaries + `appsettings.json` template filled on the server, or overrides via environment variables.

Full license rules: [docs/LICENSE_DEPLOYMENT.md](docs/LICENSE_DEPLOYMENT.md)

---

## 3. Build Release Artifacts

### Backend (API)

```powershell
cd d:\path\to\Resturant_POS
dotnet publish API\POSSystem.API.csproj -c Release -o .\publish\api
```

Output folder: `publish\api\` — copy this entire folder to the server.

### Frontend (React)

Set the API URL **at build time** if the frontend is on a different host than the API:

```powershell
cd ReactApp
npm ci
$env:VITE_API_BASE_URL="https://pos.example.com/api"   # or "/api" if same-origin reverse proxy
npm run build
```

Output folder: `ReactApp\dist\` — static files for IIS, Nginx, or CDN.

| Deployment style | `VITE_API_BASE_URL` |
|------------------|---------------------|
| Same domain, reverse proxy `/api` → API | `/api` (default) |
| Frontend and API on different domains | Full URL, e.g. `https://api.example.com/api` |

---

## 4. Production Server Configuration

Set `ASPNETCORE_ENVIRONMENT=Production` on the server. Swagger is disabled automatically in Production.

### Recommended `appsettings.json` (or env overrides)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "pos.example.com",
  "ConnectionStrings": {
    "DefaultConnection": "Server=PROD_SQL;Database=POSSystem;User Id=pos_app;Password=***;TrustServerCertificate=True;"
  },
  "Database": {
    "ApplyMigrationsOnStartup": true,
    "ApplySchemaPatchesOnStartup": true,
    "RunSeedOnStartup": false
  },
  "Jwt": {
    "Key": "GENERATE_A_NEW_32+_CHAR_RANDOM_SECRET",
    "Issuer": "POSSystem",
    "Audience": "POSSystemUsers"
  },
  "Frontend": {
    "AllowedOrigins": [
      "https://pos.example.com"
    ]
  },
  "License": {
    "Directory": "licenses",
    "FileName": "system.lic",
    "AesKeyBase64": "<production-aes-key>",
    "PublicKeyPem": "<production-public-key>",
    "AllowMissingInDevelopment": false,
    "DefaultValidityMonths": 0,
    "DefaultValidityYears": 1
  }
}
```

### Environment variable overrides (optional)

ASP.NET Core accepts nested config via double underscore:

| Setting | Environment variable |
|---------|----------------------|
| Connection string | `ConnectionStrings__DefaultConnection` |
| Run seed on startup | `Database__RunSeedOnStartup` |
| JWT secret | `Jwt__Key` |
| License AES key | `License__AesKeyBase64` |
| CORS origin (array index) | `Frontend__AllowedOrigins__0` |

> **Critical:** `AllowMissingInDevelopment` must be `false` in production. The API will not run without a valid `system.lic`.

---

## 5. Deploy to Server

### API folder layout on server

```
C:\inetpub\POSSystem\          (or /var/www/pos-api/)
├── POSSystem.API.dll
├── appsettings.json           (production values — not from dev)
├── licenses/
│   └── system.lic             (customer license file)
└── ... (published dependencies)
```

Ensure the process identity can **read/write** the `licenses/` folder (for admin UI license upload).

### Frontend

- **Option A — Same server:** Serve `ReactApp/dist` as static site; reverse-proxy `/api`, `/notificationHub`, `/orderHub` to the API.
- **Option B — Separate static host:** Deploy `dist` to IIS/Nginx/S3+CloudFront; set CORS on API and build with full `VITE_API_BASE_URL`.

### First startup

The API will on first run:

1. Create the database if it does not exist
2. Apply EF migrations (when `Database:ApplyMigrationsOnStartup` is `true`)
3. Apply idempotent schema patches (when `Database:ApplySchemaPatchesOnStartup` is `true`)
4. Load and validate `licenses/system.lic`

**Reference data is not seeded on every startup.** On a **new** customer database, run seed **once** after the API starts successfully:

| Method | When to use |
|--------|-------------|
| `Database:RunSeedOnStartup: true` for one restart | Fresh install; set back to `false` after |
| `dotnet POSSystem.API.dll --seed-database` | Scripted first deploy |
| `POST /api/database/seed` (System Admin) | Re-seed modules/menus without config change |

Seed includes: default roles, admin user (`admin` / `Admin@123`), modules, menus, units, walk-in customers, and role permissions.

Monitor logs on first deploy for migration, seed, or license errors.

---

## 6. Database Migrations (Production)

Migrations live in `Infrastructure/Migrations/`. By default the API applies them on startup when `Database:ApplyMigrationsOnStartup` is `true`.

Routine upgrades should keep `Database:RunSeedOnStartup` as **`false`** so startup stays fast and idempotent seed SQL does not run on every restart.

### Option A — Automatic (default)

Deploy the new API build and start the service. Pending migrations run on first startup. **Back up the database first.**

### Option B — Manual apply before starting the API

From the build machine (with production connection string in config or env):

```powershell
dotnet ef database update --project Infrastructure --startup-project API
```

Use when you want migrations applied while the old API is still stopped.

### Option C — SQL script (DBA / controlled rollout)

Generate a script for review, then run on production SQL Server:

```powershell
dotnet ef migrations script --project Infrastructure --startup-project API --output Infrastructure/MigrationScripts/production-update.sql
```

From last applied migration to latest only:

```powershell
dotnet ef migrations script <LastAppliedMigration> --project Infrastructure --startup-project API --output Infrastructure/MigrationScripts/incremental.sql
```

### Verify after deploy

```powershell
dotnet ef migrations list --project Infrastructure --startup-project API
```

All migrations should show as applied. Check API startup logs for `Database migrations applied.`

### Production migration checklist

- [ ] Database backed up before deploy
- [ ] New migration files included in the release build
- [ ] Tested against a staging copy of production data
- [ ] Migrations applied (auto on startup, or manual/script)
- [ ] On **new** database: seed run once (`RunSeedOnStartup`, `--seed-database`, or `POST /api/database/seed`)
- [ ] API starts without migration errors

---

## 7. License — Production Flow

Run on the **secure admin machine** (with `PrivateKeyPem` in `Tools/prod-license/appsettings.json`), not on the customer server:

```powershell
cd D:\AKHSSOFT\Projects\Resturant_POS

# Generate (must use --production)
dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --years 1 --customer "Customer Name" --max-businesses 1 --max-branches 5 --max-users 20 --output API/licenses/system.lic

# Verify before copying to server
dotnet run --project Tools/LicenseGenerator -- verify --production --config-dir API --license API/licenses/system.lic
```

Copy to server:
- `API/appsettings.json` → `PublicKeyPem` + `AesKeyBase64` (no `PrivateKeyPem`)
- `API/licenses/system.lic`

**Do not** run the generator without `--production` for customer delivery — it uses dev keys from `appsettings.Development.json` and causes `License signature verification failed` on the server.

Full guide: [docs/LICENSE_COMPLETE_SETUP.md](docs/LICENSE_COMPLETE_SETUP.md)

### Install on customer system

**Option A — Admin UI (recommended)**  
Login as System Admin → **System Settings → System License** → Upload `.lic`

**Option B — Manual**  
Copy to `API/licenses/system.lic` and restart the API, or:

```http
POST /api/licenses/reload
Authorization: Bearer <system-admin-token>
```

### Verify

```http
GET /api/licenses/status
```

Or check the **System License** page for expiry and usage limits.

---

## 8. Post-Deploy Verification Checklist

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] HTTPS enabled (TLS certificate valid)
- [ ] Frontend loads at production URL
- [ ] Login works (not blocked by license middleware)
- [ ] On fresh install: database seed completed before UAT
- [ ] `GET /api/licenses/status` returns valid, unexpired license
- [ ] CORS: browser can call API from production frontend origin
- [ ] SignalR hubs connect (`/notificationHub`, `/orderHub`) if used
- [ ] Swagger **not** exposed publicly
- [ ] Default admin password **changed** (`admin` / `Admin@123` is seeded — change immediately)
- [ ] SQL backup job scheduled
- [ ] Application logs writable and monitored

---

## 9. Security Hardening

| Item | Action |
|------|--------|
| JWT key | Unique per environment; 32+ characters |
| SQL credentials | Dedicated app login with least privilege |
| `PrivateKeyPem` | Never on production server or in Git |
| `system.lic` | Per-customer; never in Git |
| Admin password | Force change on first login |
| HTTPS | Required for production |
| Firewall | SQL Server not publicly exposed |
| File permissions | Restrict `licenses/` and config to app pool / service account |

---

## 10. Upgrade / Re-Release

```powershell
# 1. Backup production database

# 2. Stop API site/service

# 3. Publish new build (keep production appsettings.json + licenses/ on server)
dotnet publish API\POSSystem.API.csproj -c Release -o .\publish\api

# 4. Optional: apply migrations before start (production connection string required)
dotnet ef database update --project Infrastructure --startup-project API

# 5. Rebuild frontend if changed
cd ReactApp
npm ci
npm run build

# 6. Deploy dist + API folder; start service

# 7. Verify migrations and license
dotnet ef migrations list --project Infrastructure --startup-project API
```

Keep a copy of the previous `publish\api` folder and DB backup until smoke tests pass.

---

## 11. Hosting Examples

### IIS (Windows)

1. Install [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Create Application Pool — **No Managed Code**
3. Point site physical path to published API folder
4. Set `ASPNETCORE_ENVIRONMENT=Production` in App Pool → Environment Variables
5. Place frontend `dist` in a separate site or sub-application; configure URL Rewrite for SPA + API proxy

### Linux (Kestrel + systemd + Nginx)

1. Publish API to `/var/www/pos-api`
2. Run as systemd service: `dotnet POSSystem.API.dll`
3. Nginx: `proxy_pass` for `/api`, WebSocket upgrade for SignalR hubs
4. Serve `dist` from Nginx `root` or separate static location

---

## 12. Related Docs

| Document | Topic |
|----------|-------|
| [SETUP_REMINDER.md](SETUP_REMINDER.md) | Local dev setup |
| [docs/LICENSE_DEPLOYMENT.md](docs/LICENSE_DEPLOYMENT.md) | License system, signing, Git rules |
| [BUSINESS_SETUP_GUIDE.md](BUSINESS_SETUP_GUIDE.md) | Business/branch tenant API |

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| API returns license blocked / won't start | Run `verify` locally; ensure `--production` was used; keys in server `appsettings.json` must match `Tools/prod-license`; copy fresh `system.lic`; set `ASPNETCORE_ENVIRONMENT=Production` |
| 401 on all requests after deploy | Check `Jwt:Key` matches what tokens were issued with; users must re-login after JWT key change |
| CORS errors in browser | Add exact production origin (scheme + host + port) to `Frontend:AllowedOrigins` |
| Frontend calls wrong API | Rebuild with correct `VITE_API_BASE_URL`; `/api` only works with same-origin proxy |
| SignalR disconnects | Ensure reverse proxy supports WebSockets; proxy `/notificationHub` and `/orderHub` |
| DB migration fails | Backup DB; run `dotnet ef database update --project Infrastructure --startup-project API`; check SQL permissions and API logs |
| `dotnet ef` not found | On build machine: `dotnet tool install --global dotnet-ef` |
| 502 / app won't start | Verify .NET 8 runtime/hosting bundle; check `licenses/` folder exists and is readable |

---

## Release Sign-Off Template

| Step | Done | By | Date |
|------|------|-----|------|
| Build API Release | ☐ | | |
| Build frontend | ☐ | | |
| License generated & delivered | ☐ | | |
| Server config & secrets set | ☐ | | |
| Deployed & smoke tested | ☐ | | |
| Admin password changed | ☐ | | |
| DB backup confirmed | ☐ | | |

---

*Last updated: June 2026*
