# Complete License Setup — RSA Private Key to Production

Step-by-step guide: generate RSA keys, configure the app, create `.lic` files, and deploy.

---

## How it works (crypto)

```
┌─────────────────────┐         ┌──────────────────────┐
│  ADMIN MACHINE      │         │  POS SERVER (client) │
│  (License Generator)│         │                      │
├─────────────────────┤         ├──────────────────────┤
│ RSA Private Key  ◄──┼── signs   │ RSA Public Key only  │
│ RSA Public Key      │         │ AES Key (decrypt)    │
│ AES Key             │         │ system.lic file      │
└─────────┬───────────┘         └──────────┬───────────┘
          │                                │
          │  generates system.lic          │  validates + enforces limits
          └──────────────► deliver ───────►│
```

| Key | Where it lives | Purpose |
|-----|----------------|---------|
| **RSA Private Key** (`PrivateKeyPem`) | Admin / generator machine **ONLY** | Signs license files |
| **RSA Public Key** (`PublicKeyPem`) | POS server `appsettings.json` | Verifies signature |
| **AES Key** (`AesKeyBase64`) | Both generator + server | Encrypts/decrypts license payload |
| **system.lic** | POS server `API/licenses/` | Signed license file |

> **Never** put `PrivateKeyPem` on the POS server or in Git.

---

## Prerequisites

- .NET 8 SDK installed
- Repo cloned: `Resturant_POS`
- Terminal: PowerShell or CMD

---

## OPTION A — Fastest setup (development)

One command generates **RSA 2048 key pair**, **AES key**, config, and `system.lic`:

```powershell
cd D:\AKHSSOFT\Projects\Resturant_POS\Tools\LicenseGenerator
dotnet run -- init-dev
```

### What this creates

| Output | Path |
|--------|------|
| Dev config (includes private key) | `API/appsettings.Development.json` |
| Signed license file | `API/licenses/system.lic` |

### After init-dev

```powershell
cd D:\AKHSSOFT\Projects\Resturant_POS\API
dotnet run
```

Log in as **System Admin** → **System Settings → System License** to view status.

---

## OPTION B — Production setup (recommended)

Use a **separate config folder** so `appsettings.Development.json` never overrides production keys.

### Why this matters

If you run the generator from `API/` without `--production`, it loads **both**:

1. `appsettings.json`
2. `appsettings.Development.json` ← **overrides** production keys

The license is then signed with the **dev private key**, but the server uses **production public key** → API crashes on startup:

```
License signature verification failed.
```

---

### Step 1 — Create production config folder (one time)

Create **`Tools/prod-license/appsettings.json`** on your secure admin machine:

```json
{
  "License": {
    "Directory": "licenses",
    "FileName": "system.lic",
    "AesKeyBase64": "YOUR_32_BYTE_BASE64_AES_KEY",
    "PublicKeyPem": "-----BEGIN RSA PUBLIC KEY-----\nMIIBCgKCAQEA...\n-----END RSA PUBLIC KEY-----",
    "PrivateKeyPem": "-----BEGIN RSA PRIVATE KEY-----\nMIIE...\n-----END RSA PRIVATE KEY-----",
    "DefaultValidityMonths": 0,
    "DefaultValidityYears": 10
  }
}
```

Generate keys with:

```powershell
cd D:\AKHSSOFT\Projects\Resturant_POS\Tools\LicenseGenerator
dotnet run -- generate-keys
```

- Put **PublicKeyPem** + **AesKeyBase64** in `API/appsettings.json` (server deploy)
- Put **all three** (including **PrivateKeyPem**) in `Tools/prod-license/appsettings.json` (generator only)

> `Tools/prod-license/appsettings.json` is in `.gitignore` — never commit private keys.

---

### Step 2 — Configure POS server (`API/appsettings.json`)

```json
"License": {
  "Directory": "licenses",
  "FileName": "system.lic",
  "AesKeyBase64": "SAME_AES_KEY_AS_prod-license",
  "PublicKeyPem": "SAME_PUBLIC_KEY_AS_prod-license",
  "AllowMissingInDevelopment": false,
  "DefaultValidityMonths": 0,
  "DefaultValidityYears": 10
}
```

**Do not add `PrivateKeyPem` on the server.**

**Do not leave placeholders** like `REPLACE_WITH_RSA_PUBLIC_KEY_PEM` — the API will fail in production.

---

### Step 3 — Generate production license

Always use **`--production`** and **`--config-dir Tools/prod-license`**:

```powershell
cd D:\AKHSSOFT\Projects\Resturant_POS

dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --customer "Customer Name" --years 10 --output API/licenses/system.lic
```

Other examples:

```powershell
# 1 year
dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --years 1 --customer "Acme Restaurant"

# 6 months with limits
dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --months 6 --customer "Pizza Hub" --max-businesses 2 --max-branches 5 --max-users 25
```

---

### Step 4 — Verify before deploy (required)

```powershell
dotnet run --project Tools/LicenseGenerator -- verify --production --config-dir API --license API/licenses/system.lic
```

Expected output:

```
License verification OK.
  Customer: Customer Name
  Expires : 2036-06-19
```

If verify fails locally, it **will fail on the server** — fix keys before copying files.

---

### Step 5 — Deploy to server

Copy these two to the production API folder:

| Local file | Server path |
|------------|-------------|
| `API/appsettings.json` (License section) | `{API}/appsettings.json` |
| `API/licenses/system.lic` | `{API}/licenses/system.lic` |

Also ensure:

- `ASPNETCORE_ENVIRONMENT=Production`
- **Do not** deploy `appsettings.Development.json`
- Republish if using `publish/api`: `dotnet publish API/POSSystem.API.csproj -c Release -o publish/api`

---

## OPTION C — Manual setup (legacy)

Use this only if you cannot use `Tools/prod-license/`.

### Step 1 — Generate RSA key pair + AES key

```powershell
cd D:\AKHSSOFT\Projects\Resturant_POS\Tools\LicenseGenerator
dotnet run -- generate-keys
```

Console output example:

```
PUBLIC KEY:
-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEA...
...
-----END RSA PUBLIC KEY-----

PRIVATE KEY (keep in generator tool / secure vault only):
-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA...
...
-----END RSA PRIVATE KEY-----

"AesKeyBase64": "Zwe+gyEdYBwna+t5CzCuV2IbTEONJ9ygFXmRYAFP4ro="
```

**Save these three values securely:**
1. Full **PUBLIC KEY** block (including `BEGIN` / `END` lines)
2. Full **PRIVATE KEY** block (never share, never commit)
3. **AesKeyBase64** string

---

### Step 2 — Configure the generator (admin machine)

Edit **`API/appsettings.Development.json`** (local dev) or a local file **`Tools/LicenseGenerator/appsettings.local.json`** (recommended for production signing):

```json
{
  "License": {
    "Directory": "licenses",
    "FileName": "system.lic",
    "AesKeyBase64": "PASTE_YOUR_32_BYTE_BASE64_AES_KEY_HERE",
    "PublicKeyPem": "-----BEGIN RSA PUBLIC KEY-----\nMIIBCgKCAQEA...\n-----END RSA PUBLIC KEY-----",
    "PrivateKeyPem": "-----BEGIN RSA PRIVATE KEY-----\nMIIEpAIBAAKCAQEA...\n-----END RSA PRIVATE KEY-----",
    "AllowMissingInDevelopment": true,
    "DefaultValidityMonths": 0,
    "DefaultValidityYears": 10
  }
}
```

#### PEM format rules

- Use `\n` between lines **inside the JSON string**, **or** paste multi-line PEM if your editor supports it.
- Must include `-----BEGIN ... PRIVATE KEY-----` and `-----END ... PRIVATE KEY-----`.
- Do **not** leave `REPLACE_WITH_RSA_PRIVATE_KEY_PEM` — that causes:
  ```
  No supported key formats were found
  ```

---

### Step 3 — Configure the POS server (public key only)

Edit **`API/appsettings.json`** on the **server** (production):

```json
"License": {
  "Directory": "licenses",
  "FileName": "system.lic",
  "AesKeyBase64": "SAME_AES_KEY_AS_GENERATOR",
  "PublicKeyPem": "SAME_PUBLIC_KEY_AS_GENERATOR",
  "AllowMissingInDevelopment": false,
  "DefaultValidityMonths": 0,
  "DefaultValidityYears": 1
}
```

**Do not add `PrivateKeyPem` on the server.**

---

### Step 4 — Generate a license file

> **Production:** use OPTION B above (`--production --config-dir Tools/prod-license`).  
> **Development only:** commands below read `appsettings.Development.json` and may sign with dev keys.

From `Tools\LicenseGenerator`:

#### Monthly license (1 month)

```powershell
dotnet run -- --months 1 --customer "Acme Restaurant" --max-businesses 1 --max-branches 3 --max-users 10
```

#### Monthly license (6 months)

```powershell
dotnet run -- --months 6 --customer "Acme Restaurant"
```

#### Yearly license (1 year)

```powershell
dotnet run -- --years 1 --customer "Acme Restaurant"
```

#### Yearly license (10 years)

```powershell
dotnet run -- --years 10 --customer "Acme Restaurant"
```

#### Full example with all limits

```powershell
dotnet run -- --months 12 --customer "Pizza Hub" --max-businesses 2 --max-branches 5 --max-users 25 --output "D:\licenses\pizza-hub.lic"
```

### Verify generator output

Always check the console:

```
License written to D:\...\API\licenses\system.lic
Customer: Acme Restaurant
Issued : 2026-06-16
Expires: 2026-07-16          ← ~1 month later for --months 1
Period : 1 month             ← must match your command
Limits: businesses=1, branches/business=3, users=10
```

If `Period` shows `10 years (default)`, your `--months` / `--years` argument was not parsed. Use:

```powershell
dotnet run -- --months 1 --customer "Name"
```

(not `--months=1` without fixing — both `--months 1` and `--months=1` work after the latest fix)

---

### Step 5 — Install license on client POS

**Method A — Admin UI (recommended)**

1. Log in as **System Admin**
2. Go to **System Settings → System License**
3. Upload the `.lic` file
4. Limits apply **immediately** (no restart)

**Method B — Manual file copy**

1. Copy `system.lic` to `API/licenses/system.lic`
2. Restart API **or** call `POST /api/licenses/reload` as System Admin

---

### Step 6 — Verify license is active

**Browser / API:**

```
GET http://localhost:5000/api/licenses/status
```

**Admin UI:** System License page shows:
- Status: Active
- Expiry date
- Business / branch / user usage vs limits

---

## Production deployment checklist

### On secure admin machine (license signing)

- [ ] Run `dotnet run -- generate-keys` once per product/environment
- [ ] Store **PrivateKeyPem** in password manager / HSM / secure vault
- [ ] Keep generator tool with `PrivateKeyPem` in local config only
- [ ] Generate customer `.lic` files with `--months` or `--years`

### On POS server (client site)

- [ ] Set `AesKeyBase64` + `PublicKeyPem` in `appsettings.json`
- [ ] Set `AllowMissingInDevelopment: false`
- [ ] **No** `PrivateKeyPem`
- [ ] Place or upload `system.lic`
- [ ] Confirm `/api/licenses/status` returns `"isValid": true`

### Git / source control

- [ ] Push: code, docs, `appsettings.json` with placeholders
- [ ] Do **not** push: `system.lic`, `PrivateKeyPem`, real production keys
- [ ] See `docs/LICENSE_DEPLOYMENT.md` for full Git rules

---

## Command reference

| Command | Purpose |
|---------|---------|
| `dotnet run -- help` | Show all options |
| `dotnet run -- init-dev` | Auto RSA + AES + config + system.lic (dev only) |
| `dotnet run -- generate-keys` | Print new RSA public/private + AES key |
| `dotnet run -- verify --production --config-dir API --license API/licenses/system.lic` | Test license matches server config |
| `dotnet run -- --production --config-dir Tools/prod-license --years 10 --customer "X"` | **Production** license |
| `dotnet run -- --months 1 --customer "X"` | Dev license (uses Development.json) |
| `dotnet run -- --years 1 --customer "X"` | Dev license (uses Development.json) |

---

## Troubleshooting

### `License signature verification failed` (API won't start)

**Symptom:**

```
Failed to load license file.
System.InvalidOperationException: License signature verification failed.
```

**Cause:** The `system.lic` file was **not signed** with the private key that matches `PublicKeyPem` on the server. Common reasons:

| Mistake | What happened |
|---------|----------------|
| Generator run without `--production` | Signed with dev `PrivateKeyPem` from `appsettings.Development.json` |
| Different keys on server | Server `appsettings.json` has different `PublicKeyPem` / `AesKeyBase64` than used when generating |
| Placeholder keys on server | `REPLACE_WITH_RSA_PUBLIC_KEY_PEM` still in production `appsettings.json` |
| Old `system.lic` on server | New keys generated but old license file not replaced |
| Wrong PEM format on server | Public key pasted as one line without `\n` between PEM lines |
| `ASPNETCORE_ENVIRONMENT=Development` on server | Loads `appsettings.Development.json` with different keys |

**Fix (step by step):**

1. Ensure `Tools/prod-license/appsettings.json` has the **matching** private + public + AES keys.
2. Copy **PublicKeyPem** and **AesKeyBase64** from prod-license config into `API/appsettings.json`.
3. Regenerate license:
   ```powershell
   dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --customer "Customer" --years 10 --output API/licenses/system.lic
   ```
4. Verify locally:
   ```powershell
   dotnet run --project Tools/LicenseGenerator -- verify --production --config-dir API --license API/licenses/system.lic
   ```
5. Copy **both** updated `appsettings.json` (License section) and `licenses/system.lic` to server.
6. Set `ASPNETCORE_ENVIRONMENT=Production`, restart API.

**Remember:** `PrivateKeyPem` is **never** deployed to the server — only used on the admin machine to sign licenses.

---

### `No supported key formats were found`

**Cause:** `PrivateKeyPem` is still placeholder or invalid PEM.

**Fix:**

```powershell
cd Tools\LicenseGenerator
dotnet run -- init-dev
```

Or run `generate-keys` and paste real PEM blocks into config.

---

### License shows 10 years when you wanted 1 month

**Cause:** `--months` not passed correctly; generator used default.

**Fix:** Use explicit argument:

```powershell
dotnet run -- --months 1 --customer "Customer"
```

Check output line: `Period : 1 month`

---

### `PrivateKeyPem is required`

**Cause:** No private key in `appsettings.Development.json`.

**Fix:** Run `init-dev` or add `PrivateKeyPem` after `generate-keys`.

---

### API blocks all requests (license invalid)

**Cause:** Missing/invalid `system.lic` or wrong public/AES keys on server.

**Fix:**
1. Run `verify` command locally before deploy (see OPTION B)
2. Ensure `PublicKeyPem` + `AesKeyBase64` on server **match** the keys in `Tools/prod-license/appsettings.json`
3. Re-upload valid `system.lic` as System Admin or copy to `licenses/system.lic`

---

## Example: end-to-end for one customer (production)

```powershell
# 1. Admin machine — one-time keys
cd Tools\LicenseGenerator
dotnet run -- generate-keys
# → Save keys into Tools/prod-license/appsettings.json (all 3)
# → Copy PublicKeyPem + AesKeyBase64 into API/appsettings.json

# 2. Generate production license
cd D:\AKHSSOFT\Projects\Resturant_POS
dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --years 1 --customer "Karachi Biryani House" --max-businesses 1 --max-branches 5 --max-users 15

# 3. Verify before delivery
dotnet run --project Tools/LicenseGenerator -- verify --production --config-dir API --license API/licenses/system.lic

# 4. Deliver API\licenses\system.lic + appsettings.json License section to customer server
# 5. Customer: ASPNETCORE_ENVIRONMENT=Production, restart API
```

---

## Example: development only (legacy)

```powershell
cd Tools\LicenseGenerator
dotnet run -- init-dev
dotnet run -- --years 1 --customer "Local Dev Customer"
```

Uses `appsettings.Development.json` — **not** for production delivery.

---

## Related docs

- `docs/LICENSE_DEPLOYMENT.md` — Git rules, security checklist, architecture summary

---

*AKHSOFT POS — License System*
