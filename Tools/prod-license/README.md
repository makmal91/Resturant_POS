# Production license signing config

Copy `appsettings.example.json` to `appsettings.json` and fill in keys from:

```powershell
dotnet run --project Tools/LicenseGenerator -- generate-keys
```

- **PublicKeyPem** + **AesKeyBase64** → also paste into `API/appsettings.json` (server deploy)
- **PrivateKeyPem** → keep in this folder only (never deploy to server)

Generate + verify:

```powershell
dotnet run --project Tools/LicenseGenerator -- --production --config-dir Tools/prod-license --customer "Customer" --years 10 --output API/licenses/system.lic
dotnet run --project Tools/LicenseGenerator -- verify --production --config-dir API --license API/licenses/system.lic
```

See `docs/LICENSE_COMPLETE_SETUP.md` for full guide and troubleshooting.
