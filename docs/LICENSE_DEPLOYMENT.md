# License System — Deployment & Git Guide

This document explains how the Restaurant POS license system works, how to generate and install licenses, and **which files belong in Git vs. stay on the server/local machine only**.

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

Run from `Tools\LicenseGenerator`:

```powershell
dotnet run -- help
```

### Monthly license

```powershell
dotnet run -- --months 1 --customer "Customer Name"
dotnet run -- --months=6 --customer "Customer Name"
```

### Yearly license

```powershell
dotnet run -- --years 1 --customer "Customer Name"
dotnet run -- --years=10 --customer "Customer Name"
```

### With limits

```powershell
dotnet run -- --months 12 --customer "Pizza Hub" --max-businesses 2 --max-branches 5 --max-users 20
```

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

### Step 1 — Generate license (separate secure system)

1. Configure `PrivateKeyPem`, `PublicKeyPem`, and `AesKeyBase64` on the admin generator.
2. Run the generator with customer name, period, and limits.
3. Output: `system.lic`

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

# 1-month production-style license
dotnet run -- --months 1 --customer "Acme" --max-businesses 1 --max-branches 3 --max-users 10

# 1-year license
dotnet run -- --years 1 --customer "Acme"

# New RSA/AES keys (admin machine only)
dotnet run -- generate-keys
```

---

## Support Contacts

Document your internal process here:

- **License signing:** ___________________  
- **Customer delivery:** ___________________  
- **Production server config:** ___________________  

---

*Last updated: June 2026*
