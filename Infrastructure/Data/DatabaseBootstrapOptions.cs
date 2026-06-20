using Microsoft.Extensions.Configuration;

namespace POSSystem.Infrastructure.Data;

public sealed record DatabaseBootstrapOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// When false (default), the API will not run CREATE DATABASE on startup.
    /// Create the database manually in SQL Server, or set this to true once.
    /// </summary>
    public bool CreateDatabaseOnStartup { get; init; }

    /// <summary>
    /// When false (default), EF migrations are not applied on startup.
    /// Run manually: dotnet ef database update --project Infrastructure --startup-project API
    /// or pass --apply-migrations when starting the API.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; init; }

    /// <summary>
    /// When false (default), runtime SQL schema patches are not applied on startup.
    /// Pass --apply-schema-patches when starting the API, or call RunSchemaPatchesAsync.
    /// </summary>
    public bool ApplySchemaPatchesOnStartup { get; init; }

    /// <summary>
    /// When false (default), reference data and demo records are not inserted on startup.
    /// Set to true once for a fresh database, or pass --seed-database when starting the API.
    /// </summary>
    public bool RunSeedOnStartup { get; init; }

    public static DatabaseBootstrapOptions FromConfiguration(IConfiguration configuration, string[]? commandLineArgs = null)
    {
        var options = configuration.GetSection(SectionName).Get<DatabaseBootstrapOptions>()
                      ?? new DatabaseBootstrapOptions();

        if (commandLineArgs is { Length: > 0 })
        {
            if (HasFlag(commandLineArgs, "--create-database"))
                options = options with { CreateDatabaseOnStartup = true };

            if (HasFlag(commandLineArgs, "--apply-migrations"))
                options = options with { ApplyMigrationsOnStartup = true };

            if (HasFlag(commandLineArgs, "--apply-schema-patches"))
                options = options with { ApplySchemaPatchesOnStartup = true };

            if (HasFlag(commandLineArgs, "--seed-database"))
                options = options with { RunSeedOnStartup = true };
        }

        return options;
    }

    private static bool HasFlag(string[] args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
}
