# AKHSOFT POS — Setup Reminder

Quick checklist for developers after cloning the repo. Keep this file in Git; use it whenever you set up a new machine or onboard a teammate.

---

## Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ |
| [Node.js](https://nodejs.org/) | 18+ (LTS recommended) |
| SQL Server | Local or remote instance (Windows auth or SQL auth) |

---

## 1. Clone & Restore

```powershell
git clone <repository-url>
cd Resturant_POS

dotnet restore POSSystem.sln
cd ReactApp
npm install
cd ..
```

---

## 2. Git — Do Not Commit

These are ignored or must stay local. **Never push secrets or customer license files.**

| Path / item | Why |
|-------------|-----|
| `API/licenses/*.lic` | Customer-specific license files |
| `PrivateKeyPem` in any config | Used to sign licenses — admin machine only |
| Production `AesKeyBase64` / `PublicKeyPem` | Server secrets (use placeholders in `appsettings.json`) |
| `API/appsettings.Development.json` | **Only if it contains dev keys** — review before push |
| `**/appsettings.*.local.json` | Local overrides |
| `Tools/LicenseGenerator/appsettings.local.json` | Generator secrets |
| `.env`, `.env.local` | Frontend env overrides |
| `bin/`, `obj/`, `node_modules/`, `_build_out/` | Build artifacts |

**Safe to commit:** code, migrations, `API/appsettings.json` (with placeholders), `API/licenses/.gitkeep`, docs.

Full license + Git guide: [docs/LICENSE_DEPLOYMENT.md](docs/LICENSE_DEPLOYMENT.md)

---

## 3. Backend (API)

### Connection string

Edit `API/appsettings.json` (or use `API/appsettings.Development.json` for local overrides):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=POSSystem;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

The API creates the database and applies migrations on startup via `DatabaseBootstrapper`. You can also run migrations manually (see [§ Database Migrations](#database-migrations)).

### Run the API

```powershell
cd API
dotnet run
```

| Profile | URL |
|---------|-----|
| HTTP | http://localhost:5226 |
| HTTPS | https://localhost:7286 |

Swagger (Development): http://localhost:5226/swagger

---

## 4. License — First-Time Dev Setup

Required before the API will serve requests in development (unless a trial license is auto-generated):

```powershell
cd Tools\LicenseGenerator
dotnet run -- init-dev
```

This writes dev keys to `API/appsettings.Development.json` and creates `API/licenses/system.lic`.

Restart the API after running `init-dev`.

> Before pushing: remove or redact `PrivateKeyPem` from `appsettings.Development.json`.

---

## 5. Frontend (React + Vite)

```powershell
cd ReactApp
npm run dev
```

| URL | Purpose |
|-----|---------|
| http://localhost:5173 | Dev server |
| http://localhost:4173 | Preview (`npm run preview`) |

The Vite dev proxy forwards `/api` to `http://localhost:5226` (override with `VITE_API_TARGET`).

Optional env vars (create `ReactApp/.env.local` — **do not commit**):

```env
VITE_API_BASE_URL=/api
VITE_API_TARGET=http://localhost:5226
VITE_API_TIMEOUT_MS=15000
```

---

## 6. Default Login (Seeded)

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `Admin@123` |

Change this password after first login in non-dev environments.

---

## 7. Run Order Checklist

- [ ] SQL Server is running
- [ ] Connection string updated for your machine
- [ ] `dotnet run -- init-dev` completed (license tool)
- [ ] API running on port **5226**
- [ ] `npm install` done in `ReactApp`
- [ ] Frontend running on port **5173**
- [ ] Login works at http://localhost:5173

---

## 8. Solution Layout

```
Resturant_POS/
├── API/                 # ASP.NET Core 8 Web API
├── Application/         # Application services & DTOs
├── Domain/              # Domain entities
├── Infrastructure/      # EF Core, DB, license infra
├── ReactApp/            # Vite + React frontend
├── Tools/LicenseGenerator/
├── docs/                # Deployment & license docs
└── POSSystem.sln
```

---

## 9. Database Migrations

Run all commands from the **repo root**. They use `API/appsettings.json` (or `appsettings.Development.json` in Development) for the connection string.

### One-time: install EF Core CLI

```powershell
dotnet tool install --global dotnet-ef
# or update an existing install:
dotnet tool update --global dotnet-ef
```

### Add a new migration (after model/DbContext changes)

```powershell
dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project API
```

Example:

```powershell
dotnet ef migrations add AddCustomerTable --project Infrastructure --startup-project API
```

Commit the new files under `Infrastructure/Migrations/`.

### Apply migrations manually (optional — API also applies on startup)

```powershell
dotnet ef database update --project Infrastructure --startup-project API
```

### List applied / pending migrations

```powershell
dotnet ef migrations list --project Infrastructure --startup-project API
```

### Generate SQL script (review or run on another server)

```powershell
dotnet ef migrations script --project Infrastructure --startup-project API --output Infrastructure/MigrationScripts/<MigrationName>.sql
```

From a specific migration to latest:

```powershell
dotnet ef migrations script <FromMigration> --project Infrastructure --startup-project API --output Infrastructure/MigrationScripts/update.sql
```

### Remove the last migration (only if not applied to the database)

```powershell
dotnet ef migrations remove --project Infrastructure --startup-project API
```

> **Note:** On `dotnet run`, `DatabaseBootstrapper` calls `MigrateAsync()` automatically. Manual `database update` is useful when you want to migrate before starting the API or when debugging migration issues.

---

## 10. Useful Commands

```powershell
# Build entire solution
dotnet build POSSystem.sln

# Frontend production build
cd ReactApp
npm run build

# Generate a customer license (admin machine)
cd Tools\LicenseGenerator
dotnet run -- --months 12 --customer "Customer Name"
```

---

## 11. Related Docs

| Document | Topic |
|----------|-------|
| [PRODUCTION_RELEASE_REMINDER.md](PRODUCTION_RELEASE_REMINDER.md) | Production build, deploy, and release checklist |
| [docs/LICENSE_DEPLOYMENT.md](docs/LICENSE_DEPLOYMENT.md) | License system, Git rules, production deploy |
| [BUSINESS_SETUP_GUIDE.md](BUSINESS_SETUP_GUIDE.md) | Business/branch tenant API samples |
| [ReactApp/QUICK_START.md](ReactApp/QUICK_START.md) | Forms & frontend integration |

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| API blocked / license errors | Run `init-dev` or upload `system.lic` via **System Settings → System License** |
| CORS errors | Ensure frontend origin is in `Frontend:AllowedOrigins` in appsettings |
| DB connection failed | Check SQL Server name, enable TCP, use `TrustServerCertificate=True` for local dev |
| `/api` 404 from frontend | Start API first; confirm proxy target is `http://localhost:5226` |
| Migration warnings on startup | Run `dotnet ef database update --project Infrastructure --startup-project API`; check API logs |
| `dotnet ef` not found | Run `dotnet tool install --global dotnet-ef` |

---

*Last updated: June 2026*
