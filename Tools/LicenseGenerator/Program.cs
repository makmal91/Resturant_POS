using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using POSSystem.Application.License;
using POSSystem.Application.License.Models;
using POSSystem.Application.License.Options;
using POSSystem.Infrastructure.License;

if (args.Length == 0 || string.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase))
{
    PrintUsage();
    return;
}

if (args.Length > 0 && string.Equals(args[0], "generate-keys", StringComparison.OrdinalIgnoreCase))
{
    GenerateKeys();
    return;
}

if (args.Length > 0 && string.Equals(args[0], "init-dev", StringComparison.OrdinalIgnoreCase))
{
    await InitDevelopmentLicenseAsync(args);
    return;
}

var apiDirectory = ResolveApiDirectory(ReadArg(args, "--config-dir", string.Empty));
var configuration = new ConfigurationBuilder()
    .SetBasePath(apiDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var options = configuration.GetSection(LicenseOptions.SectionName).Get<LicenseOptions>() ?? new LicenseOptions();

if (string.IsNullOrWhiteSpace(options.PrivateKeyPem))
{
    Console.Error.WriteLine("PrivateKeyPem is required.");
    Console.Error.WriteLine("Run `dotnet run -- init-dev` to create keys, config, and API/licenses/system.lic.");
    PrintUsage();
    return;
}

if (string.IsNullOrWhiteSpace(options.AesKeyBase64) ||
    options.AesKeyBase64.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase))
{
    options.AesKeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    Console.WriteLine($"Generated AES key: {options.AesKeyBase64}");
}

var customerName = ReadArg(args, "--customer", "Licensed Customer");
var maxBusinesses = ReadIntArg(args, "--max-businesses", 5);
var maxBranches = ReadIntArg(args, "--max-branches", 10);
var maxUsers = ReadIntArg(args, "--max-users", 50);
var monthsValid = ReadOptionalIntArg(args, "--months", "--month", "-m");
var yearsValid = ReadOptionalIntArg(args, "--years", "--year", "-y");
WarnIfPeriodArgsWereNotParsed(args, monthsValid, yearsValid);
var defaultOutput = Path.Combine(apiDirectory, "licenses", "system.lic");
var outputPath = ReadArg(args, "--output", defaultOutput);

await WriteLicenseFileAsync(
    options,
    outputPath,
    customerName,
    maxBusinesses,
    maxBranches,
    maxUsers,
    monthsValid,
    yearsValid);

static async Task InitDevelopmentLicenseAsync(string[] args)
{
    var apiDirectory = ResolveApiDirectory(ReadArg(args, "--config-dir", string.Empty));
    var devSettingsPath = Path.Combine(apiDirectory, "appsettings.Development.json");

    using var rsa = RSA.Create(2048);
    var options = new LicenseOptions
    {
        Directory = "licenses",
        FileName = "system.lic",
        AesKeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        PublicKeyPem = rsa.ExportRSAPublicKeyPem(),
        PrivateKeyPem = rsa.ExportRSAPrivateKeyPem(),
        AllowMissingInDevelopment = true,
        DefaultValidityMonths = 0,
        DefaultValidityYears = 10
    };

    JsonObject root;
    if (File.Exists(devSettingsPath))
    {
        root = JsonNode.Parse(await File.ReadAllTextAsync(devSettingsPath))!.AsObject();
    }
    else
    {
        root = new JsonObject
        {
            ["Logging"] = new JsonObject
            {
                ["LogLevel"] = new JsonObject
                {
                    ["Default"] = "Information",
                    ["Microsoft.AspNetCore"] = "Warning"
                }
            }
        };
    }

    root["License"] = new JsonObject
    {
        ["Directory"] = options.Directory,
        ["FileName"] = options.FileName,
        ["AesKeyBase64"] = options.AesKeyBase64,
        ["PublicKeyPem"] = options.PublicKeyPem,
        ["PrivateKeyPem"] = options.PrivateKeyPem,
        ["AllowMissingInDevelopment"] = options.AllowMissingInDevelopment,
        ["DefaultValidityMonths"] = options.DefaultValidityMonths,
        ["DefaultValidityYears"] = options.DefaultValidityYears
    };

    await File.WriteAllTextAsync(
        devSettingsPath,
        root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    var licensePath = Path.Combine(apiDirectory, "licenses", "system.lic");
    await WriteLicenseFileAsync(
        options,
        licensePath,
        ReadArg(args, "--customer", "Development Tenant"),
        ReadIntArg(args, "--max-businesses", 50),
        ReadIntArg(args, "--max-branches", 50),
        ReadIntArg(args, "--max-users", 500),
        ReadOptionalIntArg(args, "--months", "--month", "-m"),
        ReadOptionalIntArg(args, "--years", "--year", "-y"));

    Console.WriteLine();
    Console.WriteLine("Development license setup complete.");
    Console.WriteLine($"  Config : {devSettingsPath}");
    Console.WriteLine($"  License: {Path.GetFullPath(licensePath)}");
    Console.WriteLine();
    Console.WriteLine("Restart the API to load the signed system.lic file.");
    Console.WriteLine("PrivateKeyPem is stored in appsettings.Development.json for local use only.");
    Console.WriteLine("Do NOT deploy PrivateKeyPem to production servers.");
}

static async Task WriteLicenseFileAsync(
    LicenseOptions options,
    string outputPath,
    string customerName,
    int maxBusinesses,
    int maxBranches,
    int maxUsers,
    int? monthsValid,
    int? yearsValid)
{
    using var privateKey = RSA.Create();
    privateKey.ImportFromPem(options.PrivateKeyPem!);

    var issuedAt = DateTime.UtcNow.Date;
    var expiresAt = LicenseValidity.ResolveExpiresAt(issuedAt, monthsValid, yearsValid, options);

    var payload = new LicensePayload
    {
        LicenseId = Guid.NewGuid().ToString("N"),
        CustomerName = customerName,
        MaxBusinesses = maxBusinesses,
        MaxBranchesPerBusiness = maxBranches,
        MaxUsers = maxUsers,
        IssuedAt = issuedAt,
        ExpiresAt = expiresAt
    };

    var document = LicenseCrypto.EncryptAndSign(payload, options, privateKey);
    var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
    if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
        Directory.CreateDirectory(outputDirectory);

    await File.WriteAllTextAsync(outputPath, LicenseCrypto.SerializeDocument(document));

    Console.WriteLine($"License written to {Path.GetFullPath(outputPath)}");
    Console.WriteLine($"Customer: {payload.CustomerName}");
    Console.WriteLine($"Issued : {payload.IssuedAt:yyyy-MM-dd}");
    Console.WriteLine($"Expires: {payload.ExpiresAt:yyyy-MM-dd}");
    Console.WriteLine($"Period : {LicenseValidity.DescribePeriod(monthsValid, yearsValid, options)}");
    Console.WriteLine($"Limits: businesses={payload.MaxBusinesses}, branches/business={payload.MaxBranchesPerBusiness}, users={payload.MaxUsers}");
}

static void PrintUsage()
{
    Console.WriteLine("License Generator");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  dotnet run -- init-dev");
    Console.WriteLine("  dotnet run -- generate-keys");
    Console.WriteLine("  dotnet run -- help");
    Console.WriteLine();
    Console.WriteLine("Generate license (monthly or yearly):");
    Console.WriteLine("  dotnet run -- --months 1 --customer \"Acme\"");
    Console.WriteLine("  dotnet run -- --months=6 --customer \"Acme\"");
    Console.WriteLine("  dotnet run -- --years 1 --customer \"Acme\"");
    Console.WriteLine("  dotnet run -- --years=10 --customer \"Acme\"");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --months, --month   Validity in months (space or equals: --months 1 / --months=1)");
    Console.WriteLine("  --years, --year     Validity in years (space or equals: --years 1 / --years=1)");
    Console.WriteLine("  --customer         Customer / tenant name");
    Console.WriteLine("  --max-businesses   Business limit");
    Console.WriteLine("  --max-branches     Branch limit per business");
    Console.WriteLine("  --max-users        User limit");
    Console.WriteLine("  --output           Output .lic path");
    Console.WriteLine("  --config-dir       Path to API folder");
    Console.WriteLine();
    Console.WriteLine("Notes:");
    Console.WriteLine("  --months takes priority over --years when both are provided.");
    Console.WriteLine("  If neither is passed, appsettings DefaultValidityMonths / DefaultValidityYears is used.");
}

static string ResolveApiDirectory(string explicitPath)
{
    if (!string.IsNullOrWhiteSpace(explicitPath))
    {
        var resolved = Path.GetFullPath(explicitPath);
        if (Directory.Exists(resolved) && File.Exists(Path.Combine(resolved, "appsettings.json")))
            return resolved;

        throw new DirectoryNotFoundException($"API config directory not found: {resolved}");
    }

    foreach (var startDirectory in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "API");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "appsettings.json")))
                return candidate;

            current = current.Parent;
        }
    }

    throw new DirectoryNotFoundException(
        "Could not locate the API project folder. Run from the repo root, or pass --config-dir <path-to-API>.");
}

static void GenerateKeys()
{
    using var rsa = RSA.Create(2048);
    Console.WriteLine("Add the following to API/appsettings.Development.json under License:");
    Console.WriteLine();
    Console.WriteLine($"\"AesKeyBase64\": \"{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}\",");
    Console.WriteLine("\"PublicKeyPem\": \"<paste public key below>\",");
    Console.WriteLine("\"PrivateKeyPem\": \"<paste private key below>\"");
    Console.WriteLine();
    Console.WriteLine("Or run: dotnet run -- init-dev");
    Console.WriteLine();
    Console.WriteLine("PUBLIC KEY:");
    Console.WriteLine(rsa.ExportRSAPublicKeyPem());
    Console.WriteLine();
    Console.WriteLine("PRIVATE KEY (keep in generator tool / secure vault only):");
    Console.WriteLine(rsa.ExportRSAPrivateKeyPem());
}

static string ReadArg(string[] args, string name, string fallback)
{
    var value = ReadOptionalArg(args, name);
    return value ?? fallback;
}

static string? ReadOptionalArg(string[] args, params string[] names)
{
    foreach (var name in names)
    {
        var prefix = name + "=";
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg[prefix.Length..];
        }

        var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0 && index + 1 < args.Length)
            return args[index + 1];
    }

    return null;
}

static int ReadIntArg(string[] args, string name, int fallback)
{
    var value = ReadOptionalIntArg(args, name);
    return value ?? fallback;
}

static int? ReadOptionalIntArg(string[] args, params string[] names)
{
    foreach (var name in names)
    {
        var prefix = name + "=";
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(arg[prefix.Length..], out var inlineValue))
            {
                return inlineValue;
            }
        }

        var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0 && index + 1 < args.Length &&
            int.TryParse(args[index + 1], out var spacedValue))
        {
            return spacedValue;
        }
    }

    return null;
}

static void WarnIfPeriodArgsWereNotParsed(string[] args, int? monthsValid, int? yearsValid)
{
    if (monthsValid.HasValue || yearsValid.HasValue)
        return;

    var rawPeriodArg = args.FirstOrDefault(arg =>
        arg.Contains("month", StringComparison.OrdinalIgnoreCase) ||
        arg.Contains("year", StringComparison.OrdinalIgnoreCase));

    if (rawPeriodArg == null)
        return;

    Console.Error.WriteLine();
    Console.Error.WriteLine("Warning: a month/year argument was found but could not be parsed.");
    Console.Error.WriteLine($"  Unparsed argument: {rawPeriodArg}");
    Console.Error.WriteLine("  Use `--months 1`, `--months=1`, `--years 1`, or `--years=1`.");
    Console.Error.WriteLine("  Falling back to appsettings default validity.");
    Console.Error.WriteLine();
}
