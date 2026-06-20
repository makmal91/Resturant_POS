# License System — Deployment & Git Guide

> **Full step-by-step setup (RSA key generation → license → deploy):** see [`LICENSE_COMPLETE_SETUP.md`](./LICENSE_COMPLETE_SETUP.md)

This document explains how the AKHSOFT POS license system works, how to generate and install licenses, and **which files belong in Git vs. stay on the server/local machine only**.

---

## Overview

| Item | Location |
|------|----------|
| License file | `API/licenses/system.lic` |
| Public crypto config | `API/appsettings.json` (production) |
| Dev overrides | `API/appsettings.Development.json` (local dev only) |
| Generator tool | `Tools/LicenseGenerator/` |
| Admin upload UI | **System Settings → System License** (`/settings/licenses`) |

The license is **file-based** (not stored in the database). Limits are enforced via middleware and services for business, branch, and user creation.

---

## Git: Push vs. Keep Local

### Safe to push to Git

| Path | Reason |
|------|--------|
| `Application/License/**` | License models, options, enforcement logic |
| `Infrastructure/License/**` | `LicenseService`, crypto, usage provider |
| `API/Middleware/License*.cs` | Gate and enforcement middleware |
| `API/Controllers/LicensesController.cs` | Upload/status API |
| `Tools/LicenseGenerator/**` | External license generator (no secrets in code) |
| `ReactApp/src/modules/settings/LicensePage.tsx` | Admin UI |
| `docs/LICENSE_DEPLOYMENT.md` | This document |
| `API/licenses/.gitkeep` | Keeps empty licenses folder in repo |
| `API/appsettings.json` | Template with **placeholder** keys only |

**`appsettings.json` License section (template only):**

```json
"License": {
  "Directory": "licenses",
  "FileName": "system.lic",
  "AesKeyBase64": "REPLACE_WITH_BASE64_32_BYTE_AES_KEY",
  "PublicKeyPem": "REPLACE_WITH_RSA_PUBLIC_KEY_PEM",
  "AllowMissingInDevelopment": true,
  "DefaultValidityMonths": 0,
  "DefaultValidityYears": 10
}
```

---

### Do NOT push to Git (keep local / server only)

| Path / Secret | Reason |
|---------------|--------|
| `API/licenses/system.lic` | Customer-specific signed license |
| `PrivateKeyPem` | Used to **sign** licenses — must never be on the app server or in Git |
| Production `AesKeyBase64` | Decrypts license payload on the server |
| Production `PublicKeyPem` | Verifies signature (safe on server, but pair with your signing key offline) |
| `API/appsettings.Development.json` | **If it contains `PrivateKeyPem`** — dev keys only |
| Customer `.lic` exports | Deliver via portal, email, or secure channel — not Git |

> **Rule:** The API server only needs **PublicKeyPem + AesKeyBase64**. The **private key** lives only in your secure admin/license generator machine.

These paths are listed in `.gitignore`:

```
API/licenses/*.lic
!API/licenses/.gitkeep
Tools/prod-license/appsettings.json
Tools/prod-license/licenses/
```

---

## One-Time Setup (Development)

From the repo root:

```powershell
cd Tools\LicenseGenerator
dotnet run -- init-dev
```

This creates:

- RSA + AES keys in `API/appsettings.Development.json`
- Signed `API/licenses/system.lic`

Restart the API after generation.

> **Before pushing to Git:** Remove or redact `PrivateKeyPem` from `appsettings.Development.json`, or do not commit that file if it contains secrets.

---

## Generate Licenses (Admin Tool)

Run from repo root or `Tools\LicenseGenerator`:

```powershell
dotnet run --project Tools/LicenseGenerator -- help
```

### Production license (required for server deploy)

Use a dedicated config folder with **private key** — never read `appsettings.Development.json`:

```powershell
# 1. Create Tools/prod-license/appsettings.json (see LICENSE_COMPLETE_SETUP.md)

# 2. Generate
dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --years 10 --customer "Customer Name" --output API/licenses/system.lic

# 3. Verify before deploy
dotnet run --project Tools/LicenseGenerator -- verify --production --config-dir API --license API/licenses/system.lic
```

### Development license (local only)

```powershell
cd Tools\LicenseGenerator
dotnet run -- init-dev
dotnet run -- --months 1 --customer "Customer Name"
```

> **Warning:** Commands without `--production` load `appsettings.Development.json` and sign with dev keys. That license will **not** work on a production server with different keys.

### Important syntax

- Always use **`dotnet run --`** before arguments.
- Both formats work: `--months 1` and `--months=1`.
- Check console output:
  - `Period : 1 month` (or `1 year`)
  - `Expires:` should match the period you requested.

### Generate new key pair (offline admin machine)

```powershell
dotnet run -- generate-keys
```

Copy **PublicKeyPem** and **AesKeyBase64** to production `appsettings.json`.  
Keep **PrivateKeyPem** only on the license generator machine (never on the POS server).

---

## Production Deployment Flow

### Step 1 — Generate license (secure admin machine)

1. Create `Tools/prod-license/appsettings.json` with `PrivateKeyPem`, `PublicKeyPem`, and `AesKeyBase64`.
2. Copy `PublicKeyPem` + `AesKeyBase64` into `API/appsettings.json` (no private key).
3. Generate with `--production`:
   ```powershell
   dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --years 1 --customer "Customer Name"
   ```
4. Verify:
   ```powershell
   dotnet run --project Tools/LicenseGenerator -- verify --production --config-dir API --license API/licenses/system.lic
   ```
5. Output: `API/licenses/system.lic`

### Step 2 — Deliver license to client

- Portal download, email, WhatsApp, or secure admin handoff  
- **Do not** commit the `.lic` file to Git

### Step 3 — Configure production server

On the server, set in `appsettings.json` (or environment / secrets vault):

```json
"License": {
  "Directory": "licenses",
  "FileName": "system.lic",
  "AesKeyBase64": "<production-aes-key>",
  "PublicKeyPem": "<production-public-key>",
  "AllowMissingInDevelopment": false,
  "DefaultValidityMonths": 0,
  "DefaultValidityYears": 1
}
```

- **No `PrivateKeyPem`** on production.
- Place `system.lic` in `API/licenses/` **or** upload via admin UI.

Also set database startup (see [SETUP_REMINDER.md](../SETUP_REMINDER.md) and [PRODUCTION_RELEASE_REMINDER.md](../PRODUCTION_RELEASE_REMINDER.md)):

```json
"Database": {
  "ApplyMigrationsOnStartup": true,
  "ApplySchemaPatchesOnStartup": true,
  "RunSeedOnStartup": false
}
```

On a **new** database, run seed once after first successful API start (`RunSeedOnStartup: true` for one restart, `dotnet POSSystem.API.dll --seed-database`, or `POST /api/database/seed` as System Admin). Keep `RunSeedOnStartup` `false` for routine restarts.

### Step 4 — Install license (client system)

**Option A — Admin UI (recommended)**  
System Admin → **System Settings → System License** → Upload `.lic`

**Option B — Manual**  
Copy `system.lic` to `API/licenses/` and restart the API, or call:

```http
POST /api/licenses/reload
```

(Requires System Admin authentication.)

### Step 5 — Verify

```http
GET /api/licenses/status
```

Or check **System License** page for status, expiry, and usage counts.

---

## Default Validity (when `--months` / `--years` omitted)

Configured in `appsettings`:

| Setting | Meaning |
|---------|---------|
| `DefaultValidityMonths: 6` | Default = 6 months (takes priority if &gt; 0) |
| `DefaultValidityMonths: 0` | Use years instead |
| `DefaultValidityYears: 10` | Default = 10 years |

Priority when generating:

1. `--months`
2. `--years`
3. `DefaultValidityMonths`
4. `DefaultValidityYears`

---

## Enforcement Rules

Applied on **POST** create for:

| Operation | Limit field | Error message |
|-----------|-------------|---------------|
| Business | `MaxBusinesses` | Business limit reached |
| Branch | `MaxBranchesPerBusiness` | Branch limit reached |
| User | `MaxUsers` | User limit reached |

Enforced by:

- `LicenseEnforcementMiddleware` (API layer)
- `BusinessService`, `BranchService`, `UserService` (service layer)

---

## System Behavior

| Event | Behavior |
|-------|----------|
| Startup | Load `licenses/system.lic`, decrypt, verify signature, cache in memory |
| Invalid / expired license | Block API (except login, status, System Admin upload) |
| License upload | Validate, replace file, reload cache **without restart** |
| Development, no file | In-memory trial **or** auto-generate file if dev keys configured |

---

## Security Checklist

- [ ] `PrivateKeyPem` never on production server or in Git  
- [ ] `system.lic` not in Git (per-customer file)  
- [ ] Production `AllowMissingInDevelopment: false`  
- [ ] Only **System Admin** can upload licenses  
- [ ] License limits not stored in DB (cannot be bypassed via SQL)  
- [ ] Signed `.lic` cannot be edited manually without invalidating signature  

---

## Quick Reference — Commands

```powershell
# First-time dev setup
cd Tools\LicenseGenerator
dotnet run -- init-dev

# Production license (use prod-license config folder)
dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --years 1 --customer "Acme"

# Verify license matches API appsettings before deploy
dotnet run --project Tools/LicenseGenerator -- verify --production --config-dir API --license API/licenses/system.lic

# New RSA/AES keys (admin machine only)
dotnet run --project Tools/LicenseGenerator -- generate-keys
```

---

## Common production error

**`License signature verification failed`** — the `.lic` file was signed with a different private key than the `PublicKeyPem` on the server.

Typical causes:
- Generator run without `--production` (used dev keys from `appsettings.Development.json`)
- Server `appsettings.json` still has `REPLACE_WITH_...` placeholders
- Old `system.lic` not replaced after generating new keys
- `ASPNETCORE_ENVIRONMENT=Development` on server loading wrong config

Full fix: [LICENSE_COMPLETE_SETUP.md — Troubleshooting](./LICENSE_COMPLETE_SETUP.md#license-signature-verification-failed-api-wont-start)

---

## Support Contacts

Document your internal process here:

- **License signing:** ___________________  
- **Customer delivery:** ___________________  
- **Production server config:** ___________________  

---

*Last updated: June 2026 — added `--production`, `verify`, signature troubleshooting*
