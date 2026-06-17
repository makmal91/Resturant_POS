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

## OPTION B — Manual setup (production-style)

Use this when you want full control or a **production key pair** on a secure admin PC.

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
| `dotnet run -- init-dev` | Auto RSA + AES + config + system.lic (dev) |
| `dotnet run -- generate-keys` | Print new RSA public/private + AES key |
| `dotnet run -- --months 1 --customer "X"` | 1-month license |
| `dotnet run -- --years 1 --customer "X"` | 1-year license |

---

## Troubleshooting

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
1. Ensure `PublicKeyPem` + `AesKeyBase64` on server **match** the generator keys used to sign the `.lic`
2. Re-upload valid `system.lic` as System Admin

---

## Example: end-to-end for one customer

```powershell
# 1. Admin machine — one-time keys
cd Tools\LicenseGenerator
dotnet run -- generate-keys
# → Save public key, private key, AES key

# 2. Paste keys into appsettings.Development.json (generator config)

# 3. Generate 1-year license for customer
dotnet run -- --years 1 --customer "Karachi Biryani House" --max-businesses 1 --max-branches 5 --max-users 15

# 4. Deliver API\licenses\system.lic to customer (email/USB/portal)

# 5. Customer server — appsettings.json with PublicKeyPem + AesKeyBase64 only
# 6. Customer uploads .lic via System Admin → System License
```

---

## Related docs

- `docs/LICENSE_DEPLOYMENT.md` — Git rules, security checklist, architecture summary

---

*AKHSOFT POS — License System*
