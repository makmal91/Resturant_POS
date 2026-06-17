using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using POSSystem.Application.License;
using POSSystem.Application.License.DTOs;
using POSSystem.Application.License.Interfaces;
using POSSystem.Application.License.Models;
using POSSystem.Application.License.Options;

namespace POSSystem.Infrastructure.License;

public sealed class LicenseService : ILicenseService
{
    private readonly LicenseOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LicenseService> _logger;
    private readonly object _sync = new();
    private LicenseSnapshot? _current;

    public LicenseService(
        IOptions<LicenseOptions> options,
        IHostEnvironment environment,
        ILogger<LicenseService> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public LicenseSnapshot? Current
    {
        get
        {
            lock (_sync)
            {
                return _current;
            }
        }
    }

    public bool IsOperational
    {
        get
        {
            lock (_sync)
            {
                return _current is { IsValid: true, IsExpired: false };
            }
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureLicenseDirectoryExists();

        if (!File.Exists(GetLicenseFilePath()) &&
            _environment.IsDevelopment() &&
            _options.AllowMissingInDevelopment)
        {
            if (HasValidCryptoConfiguration() && !string.IsNullOrWhiteSpace(_options.PrivateKeyPem))
            {
                EnsureDevelopmentLicenseFile();
                return Task.CompletedTask;
            }

            ApplyDevelopmentTrialLicense();
            _logger.LogWarning(
                "No signed system.lic found. Running with in-memory development trial. " +
                "Run `dotnet run --project Tools/LicenseGenerator -- init-dev` to generate API/licenses/system.lic.");
            return Task.CompletedTask;
        }

        ReloadInternal();

        if (!IsOperational && !_environment.IsDevelopment())
            throw new InvalidOperationException(GetStatus().Message ?? "System license is invalid or expired.");

        return Task.CompletedTask;
    }

    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReloadInternal();
        return Task.CompletedTask;
    }

    public async Task InstallLicenseFileAsync(Stream licenseStream, CancellationToken cancellationToken = default)
    {
        if (licenseStream == null || !licenseStream.CanRead)
            throw new InvalidOperationException("License file is required.");

        using var reader = new StreamReader(licenseStream, leaveOpen: false);
        var content = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("License file is empty.");

        var payload = LoadPayloadFromContent(content);

        EnsureLicenseDirectoryExists();
        await File.WriteAllTextAsync(GetLicenseFilePath(), content, cancellationToken);
        ApplyLoadedPayload(payload);

        _logger.LogInformation("License installed for {CustomerName}.", payload.CustomerName);
    }

    public LicenseStatusDto GetStatus()
    {
        var snapshot = Current;
        if (snapshot?.Payload == null || !snapshot.IsValid)
        {
            return new LicenseStatusDto
            {
                IsValid = false,
                IsExpired = snapshot?.IsExpired ?? false,
                Message = snapshot?.ValidationMessage ?? "License is not loaded.",
                LoadedAt = snapshot?.LoadedAt
            };
        }

        return new LicenseStatusDto
        {
            IsValid = snapshot.IsValid,
            IsExpired = snapshot.IsExpired,
            Message = snapshot.ValidationMessage,
            LicenseId = snapshot.Payload.LicenseId,
            CustomerName = snapshot.Payload.CustomerName,
            IssuedAt = snapshot.Payload.IssuedAt,
            ExpiresAt = snapshot.Payload.ExpiresAt,
            MaxBusinesses = snapshot.Payload.MaxBusinesses,
            MaxBranchesPerBusiness = snapshot.Payload.MaxBranchesPerBusiness,
            MaxUsers = snapshot.Payload.MaxUsers,
            LoadedAt = snapshot.LoadedAt
        };
    }

    public LicensePayload? GetActivePayload()
    {
        var snapshot = Current;
        if (snapshot is not { IsValid: true, IsExpired: false })
            return null;

        return snapshot.Payload;
    }

    private void ReloadInternal()
    {
        var filePath = GetLicenseFilePath();
        if (!File.Exists(filePath))
        {
            SetSnapshot(new LicenseSnapshot
            {
                IsValid = false,
                IsExpired = false,
                ValidationMessage = "License file not found.",
                LoadedAt = DateTime.UtcNow
            });
            return;
        }

        try
        {
            var content = File.ReadAllText(filePath);
            var payload = LoadPayloadFromContent(content);
            ApplyLoadedPayload(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load license file.");
            SetSnapshot(new LicenseSnapshot
            {
                IsValid = false,
                IsExpired = false,
                ValidationMessage = ex.Message,
                LoadedAt = DateTime.UtcNow
            });
        }
    }

    private LicensePayload LoadPayloadFromContent(string content)
    {
        ValidateCryptoConfiguration();

        var document = LicenseCrypto.ParseDocument(content);
        return LicenseCrypto.DecryptAndVerify(document, _options);
    }

    private void ApplyLoadedPayload(LicensePayload payload)
    {
        var isExpired = payload.ExpiresAt.ToUniversalTime() < DateTime.UtcNow;

        SetSnapshot(new LicenseSnapshot
        {
            Payload = payload,
            LoadedAt = DateTime.UtcNow,
            IsValid = true,
            IsExpired = isExpired,
            ValidationMessage = isExpired ? "License has expired." : null
        });

        _logger.LogInformation(
            "License loaded for {CustomerName}. Expires {ExpiresAt:u}.",
            payload.CustomerName,
            payload.ExpiresAt);
    }

    private void ApplyDevelopmentTrialLicense()
    {
        SetSnapshot(new LicenseSnapshot
        {
            Payload = new LicensePayload
            {
                LicenseId = "DEV-TRIAL",
                CustomerName = "Development Trial",
                MaxBusinesses = 50,
                MaxBranchesPerBusiness = 50,
                MaxUsers = 500,
                IssuedAt = DateTime.UtcNow.Date,
                ExpiresAt = DateTime.UtcNow.Date.AddYears(_options.DefaultValidityYears > 0 ? _options.DefaultValidityYears : 10)
            },
            LoadedAt = DateTime.UtcNow,
            IsValid = true,
            IsExpired = false,
            ValidationMessage = "Development trial license (in-memory only)."
        });
    }

    private void EnsureDevelopmentLicenseFile()
    {
        using var privateKey = RSA.Create();
        privateKey.ImportFromPem(_options.PrivateKeyPem!);

        var issuedAt = DateTime.UtcNow.Date;
        var payload = new LicensePayload
        {
            LicenseId = Guid.NewGuid().ToString("N"),
            CustomerName = "Development Tenant",
            MaxBusinesses = 50,
            MaxBranchesPerBusiness = 50,
            MaxUsers = 500,
            IssuedAt = issuedAt,
            ExpiresAt = LicenseValidity.ResolveExpiresAt(issuedAt, null, null, _options)
        };

        var document = LicenseCrypto.EncryptAndSign(payload, _options, privateKey);
        File.WriteAllText(GetLicenseFilePath(), LicenseCrypto.SerializeDocument(document));
        ApplyLoadedPayload(payload);

        _logger.LogInformation("Generated development license file at {Path}.", GetLicenseFilePath());
    }

    private bool HasValidCryptoConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.AesKeyBase64) ||
            string.IsNullOrWhiteSpace(_options.PublicKeyPem) ||
            _options.AesKeyBase64.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase) ||
            _options.PublicKeyPem.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            if (Convert.FromBase64String(_options.AesKeyBase64).Length != 32)
                return false;

            using var rsa = RSA.Create();
            rsa.ImportFromPem(_options.PublicKeyPem);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ValidateCryptoConfiguration()
    {
        if (!HasValidCryptoConfiguration())
            throw new InvalidOperationException("License crypto settings are missing or invalid.");
    }

    private void EnsureLicenseDirectoryExists()
    {
        var directory = GetLicenseDirectoryPath();
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    private string GetLicenseDirectoryPath()
        => Path.IsPathRooted(_options.Directory)
            ? _options.Directory
            : Path.Combine(_environment.ContentRootPath, _options.Directory);

    private string GetLicenseFilePath()
        => Path.Combine(GetLicenseDirectoryPath(), _options.FileName);

    private void SetSnapshot(LicenseSnapshot snapshot)
    {
        lock (_sync)
        {
            _current = snapshot;
        }
    }
}
