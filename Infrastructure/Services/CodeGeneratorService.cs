using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Services;

public class CodeGeneratorService : ICodeGeneratorService
{
    private readonly POSDbContext _context;

    private sealed record ModuleConfig(string Prefix, int PadLength, CodeResetType ResetType, bool IsGlobal);

    private static readonly Dictionary<string, ModuleConfig> ModuleConfigs = new(StringComparer.OrdinalIgnoreCase)
    {
        [CodeModuleNames.Branch]       = new("BR",  4, CodeResetType.None,    true),
        [CodeModuleNames.Category]     = new("CAT", 4, CodeResetType.None,    false),
        [CodeModuleNames.SubCategory]  = new("SUB", 4, CodeResetType.None,    false),
        [CodeModuleNames.Product]      = new("PRD", 5, CodeResetType.None,    false),
        [CodeModuleNames.Customer]     = new("CUS", 5, CodeResetType.None,    false),
        [CodeModuleNames.Supplier]     = new("SUP", 5, CodeResetType.None,    false),
        [CodeModuleNames.Purchase]         = new("PUR", 4, CodeResetType.Monthly, false),
        [CodeModuleNames.SalesInvoice]     = new("INV", 4, CodeResetType.Daily,   false),
        [CodeModuleNames.CustomerReceipt]  = new("REC", 4, CodeResetType.Daily,   false),
        [CodeModuleNames.SupplierPayment]  = new("PAY", 4, CodeResetType.Daily,   false),
        [CodeModuleNames.Expense]          = new("EXP", 4, CodeResetType.Daily,   false),
        [CodeModuleNames.JournalVoucher]   = new("JV",  4, CodeResetType.Daily,   false),
        [CodeModuleNames.OpeningStock]     = new("OS",  5, CodeResetType.None,    false),
        [CodeModuleNames.StockTransfer]    = new("ST",  5, CodeResetType.None,    false),
        [CodeModuleNames.StockAdjustment]  = new("SA",  5, CodeResetType.None,    false),
    };

    public CodeGeneratorService(POSDbContext context) => _context = context;

    /// <summary>
    /// Reserves the next code by updating the tracked sequence entity.
    /// Does not call SaveChanges — the caller must persist in the same unit of work.
    /// </summary>
    public Task<string> GenerateAsync(string moduleName, int? branchId = null, CancellationToken cancellationToken = default)
        => NextCodeAsync(moduleName, branchId, increment: true, cancellationToken);

    /// <summary>Read-only preview of the next code; never writes to the database.</summary>
    public Task<string> PreviewAsync(string moduleName, int? branchId = null, CancellationToken cancellationToken = default)
        => NextCodeAsync(moduleName, branchId, increment: false, cancellationToken);

    public async Task<string> ResolveAsync(
        string moduleName,
        int? branchId,
        string? requestedCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestedCode))
            return await GenerateAsync(moduleName, branchId, cancellationToken);

        var code = requestedCode.Trim();
        var preview = await PreviewAsync(moduleName, branchId, cancellationToken);
        if (string.Equals(code, preview, StringComparison.OrdinalIgnoreCase))
            return await GenerateAsync(moduleName, branchId, cancellationToken);

        if (!ModuleConfigs.TryGetValue(moduleName, out var config))
            throw new InvalidOperationException($"Unknown code module '{moduleName}'.");

        if (TryParseSequenceNumber(moduleName, code, config, out var parsedNumber)
            && IsCodeInActivePeriod(moduleName, code))
        {
            await SyncSequenceToNumberAsync(moduleName, branchId, config, parsedNumber, cancellationToken);
        }

        return code;
    }

    public async Task<string> GenerateBarcodeAsync(int businessId, int branchId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            var candidate = GenerateEan13();
            var exists = await _context.ProductBarcodes
                .IgnoreQueryFilters()
                .AnyAsync(b => !b.IsDeleted && b.BarcodeValue == candidate, cancellationToken);

            if (!exists)
                return candidate;
        }

        throw new InvalidOperationException("Unable to generate a unique barcode. Please try again.");
    }

    private async Task<string> NextCodeAsync(
        string moduleName,
        int? branchId,
        bool increment,
        CancellationToken cancellationToken)
    {
        if (!ModuleConfigs.TryGetValue(moduleName, out var config))
            throw new InvalidOperationException($"Unknown code module '{moduleName}'.");

        if (config.IsGlobal)
            branchId = null;
        else if (!branchId.HasValue || branchId.Value <= 0)
            throw new InvalidOperationException($"BranchId is required for module '{moduleName}'.");

        if (increment)
        {
            var sequence = await GetOrCreateSequenceForUpdateAsync(moduleName, branchId, config, cancellationToken);
            ApplyResetIfNeeded(sequence, config.ResetType);

            var nextNumber = sequence.LastNumber + 1;
            sequence.LastNumber = nextNumber;
            sequence.LastResetDate = DateTime.UtcNow;

            return FormatCode(moduleName, config, nextNumber);
        }

        var effectiveLastNumber = await GetEffectiveLastNumberAsync(moduleName, branchId, config, cancellationToken);
        return FormatCode(moduleName, config, effectiveLastNumber + 1);
    }

    private async Task<long> GetEffectiveLastNumberAsync(
        string moduleName,
        int? branchId,
        ModuleConfig config,
        CancellationToken cancellationToken)
    {
        var previewSequence = await _context.CodeSequences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.ModuleName == moduleName && s.BranchId == branchId,
                cancellationToken);

        var maxFromData = await GetMaxUsedNumberFromDataAsync(moduleName, branchId, config, cancellationToken);

        if (previewSequence == null)
            return maxFromData;

        var effective = new CodeSequence
        {
            LastNumber = Math.Max(previewSequence.LastNumber, maxFromData),
            LastResetDate = previewSequence.LastResetDate,
            ResetType = previewSequence.ResetType
        };
        ApplyResetIfNeeded(effective, config.ResetType);
        return effective.LastNumber;
    }

    private async Task<CodeSequence> GetOrCreateSequenceForUpdateAsync(
        string moduleName,
        int? branchId,
        ModuleConfig config,
        CancellationToken cancellationToken)
    {
        CodeSequence? sequence;

        if (branchId == null)
        {
            sequence = await _context.CodeSequences
                .FromSqlInterpolated(
                    $@"SELECT * FROM CodeSequences WITH (UPDLOCK, HOLDLOCK)
                       WHERE ModuleName = {moduleName} AND BranchId IS NULL")
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            sequence = await _context.CodeSequences
                .FromSqlInterpolated(
                    $@"SELECT * FROM CodeSequences WITH (UPDLOCK, HOLDLOCK)
                       WHERE ModuleName = {moduleName} AND BranchId = {branchId.Value}")
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (sequence == null)
        {
            sequence = new CodeSequence
            {
                ModuleName = moduleName,
                BranchId = branchId,
                Prefix = config.Prefix,
                LastNumber = 0,
                ResetType = config.ResetType,
                LastResetDate = DateTime.UtcNow
            };
            _context.CodeSequences.Add(sequence);
        }

        await ReconcileSequenceWithExistingDataAsync(sequence, moduleName, branchId, config, cancellationToken);
        return sequence;
    }

    private async Task ReconcileSequenceWithExistingDataAsync(
        CodeSequence sequence,
        string moduleName,
        int? branchId,
        ModuleConfig config,
        CancellationToken cancellationToken)
    {
        var maxFromData = await GetMaxUsedNumberFromDataAsync(moduleName, branchId, config, cancellationToken);
        if (maxFromData > sequence.LastNumber)
        {
            sequence.LastNumber = maxFromData;
            sequence.LastResetDate = DateTime.UtcNow;
        }
    }

    private async Task<long> GetMaxUsedNumberFromDataAsync(
        string moduleName,
        int? branchId,
        ModuleConfig config,
        CancellationToken cancellationToken)
    {
        var numberStart = config.Prefix.Length + 2;
        var padLength = config.PadLength;
        var now = DateTime.UtcNow;

        FormattableString sql = moduleName switch
        {
            CodeModuleNames.Branch =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([Code], {numberStart}, 20) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [Branches]
                 WHERE [IsDeleted] = 0 AND [Code] LIKE {config.Prefix + "-%"}
                 """,

            CodeModuleNames.Category =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([Code], {numberStart}, 20) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [MenuCategories]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value} AND [Code] LIKE {config.Prefix + "-%"}
                 """,

            CodeModuleNames.SubCategory =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([Code], {numberStart}, 20) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [SubCategories]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value} AND [Code] LIKE {config.Prefix + "-%"}
                 """,

            CodeModuleNames.Product =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([ProductCode], {numberStart}, 20) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [Products]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value} AND [ProductCode] LIKE {config.Prefix + "-%"}
                 """,

            CodeModuleNames.Customer =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([CustomerCode], {numberStart}, 20) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [Customers]
                 WHERE [IsDeleted] = 0 AND [IsWalkIn] = 0 AND [BranchId] = {branchId!.Value}
                   AND [CustomerCode] LIKE {config.Prefix + "-%"}
                 """,

            CodeModuleNames.Supplier =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([SupplierCode], {numberStart}, 20) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [Suppliers]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value} AND [SupplierCode] LIKE {config.Prefix + "-%"}
                 """,

            CodeModuleNames.Purchase =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(RIGHT([InvoiceNo], {padLength}) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [Purchases]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value}
                   AND [InvoiceNo] LIKE {$"{config.Prefix}-{now:yyyyMM}-%"}
                 """,

            CodeModuleNames.SalesInvoice =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(RIGHT([InvoiceNo], {padLength}) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [SaleInvoices]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value}
                   AND [InvoiceNo] LIKE {$"{config.Prefix}-{now:yyyyMMdd}-%"}
                 """,

            CodeModuleNames.CustomerReceipt =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(RIGHT([ReferenceNo], {padLength}) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [InvoicePayments]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value} AND [Module] = 1
                   AND [ReferenceNo] LIKE {$"{config.Prefix}-{now:yyyyMMdd}-%"}
                 """,

            CodeModuleNames.SupplierPayment =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(RIGHT([ReferenceNo], {padLength}) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [InvoicePayments]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value} AND [Module] = 2
                   AND [ReferenceNo] LIKE {$"{config.Prefix}-{now:yyyyMMdd}-%"}
                 """,

            CodeModuleNames.Expense =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(RIGHT([ReferenceNo], {padLength}) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [Expenses]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value}
                   AND [ReferenceNo] LIKE {$"{config.Prefix}-{now:yyyyMMdd}-%"}
                 """,

            CodeModuleNames.JournalVoucher =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(RIGHT([VoucherNo], {padLength}) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [JournalVouchers]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value}
                   AND [VoucherNo] LIKE {$"{config.Prefix}-{now:yyyyMMdd}-%"}
                 """,

            CodeModuleNames.OpeningStock =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([VoucherNo], {numberStart}, 20) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [OpeningStockVouchers]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value} AND [VoucherNo] LIKE {config.Prefix + "-%"}
                 """,

            CodeModuleNames.StockTransfer =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([TransferNo], {numberStart}, 20) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [StockTransferVouchers]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value} AND [TransferNo] LIKE {config.Prefix + "-%"}
                 """,

            CodeModuleNames.StockAdjustment =>
                $"""
                 SELECT ISNULL(MAX(TRY_CAST(SUBSTRING([AdjustmentNo], {numberStart}, 20) AS bigint)), CAST(0 AS bigint)) AS [Value]
                 FROM [StockAdjustments]
                 WHERE [IsDeleted] = 0 AND [BranchId] = {branchId!.Value} AND [AdjustmentNo] LIKE {config.Prefix + "-%"}
                 """,

            _ => throw new InvalidOperationException($"Unknown code module '{moduleName}'.")
        };

        return await _context.Database.SqlQuery<long>(sql).SingleAsync(cancellationToken);
    }

    private async Task SyncSequenceToNumberAsync(
        string moduleName,
        int? branchId,
        ModuleConfig config,
        long parsedNumber,
        CancellationToken cancellationToken)
    {
        if (parsedNumber <= 0)
            return;

        if (config.IsGlobal)
            branchId = null;
        else if (!branchId.HasValue || branchId.Value <= 0)
            throw new InvalidOperationException($"BranchId is required for module '{moduleName}'.");

        var sequence = await GetOrCreateSequenceForUpdateAsync(moduleName, branchId, config, cancellationToken);
        ApplyResetIfNeeded(sequence, config.ResetType);

        if (parsedNumber > sequence.LastNumber)
        {
            sequence.LastNumber = parsedNumber;
            sequence.LastResetDate = DateTime.UtcNow;
        }
    }

    private static bool TryParseSequenceNumber(string moduleName, string code, ModuleConfig config, out long number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var parts = code.Split('-', StringSplitOptions.TrimEntries);

        return moduleName switch
        {
            CodeModuleNames.Purchase when parts.Length == 3
                && parts[0].Equals(config.Prefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(parts[2], out number) => true,
            CodeModuleNames.SalesInvoice when parts.Length == 3
                && parts[0].Equals(config.Prefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(parts[2], out number) => true,
            CodeModuleNames.CustomerReceipt when parts.Length == 3
                && parts[0].Equals(config.Prefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(parts[2], out number) => true,
            CodeModuleNames.SupplierPayment when parts.Length == 3
                && parts[0].Equals(config.Prefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(parts[2], out number) => true,
            CodeModuleNames.Expense when parts.Length == 3
                && parts[0].Equals(config.Prefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(parts[2], out number) => true,
            CodeModuleNames.JournalVoucher when parts.Length == 3
                && parts[0].Equals(config.Prefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(parts[2], out number) => true,
            _ when parts.Length == 2
                && parts[0].Equals(config.Prefix, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(parts[1], out number) => true,
            _ => false
        };
    }

    private static bool IsCodeInActivePeriod(string moduleName, string code)
    {
        var parts = code.Split('-', StringSplitOptions.TrimEntries);
        var now = DateTime.UtcNow;

        return moduleName switch
        {
            CodeModuleNames.Purchase when parts.Length == 3
                => parts[1] == now.ToString("yyyyMM"),
            CodeModuleNames.SalesInvoice when parts.Length == 3
                => parts[1] == now.ToString("yyyyMMdd"),
            CodeModuleNames.CustomerReceipt when parts.Length == 3
                => parts[1] == now.ToString("yyyyMMdd"),
            CodeModuleNames.SupplierPayment when parts.Length == 3
                => parts[1] == now.ToString("yyyyMMdd"),
            CodeModuleNames.Expense when parts.Length == 3
                => parts[1] == now.ToString("yyyyMMdd"),
            CodeModuleNames.JournalVoucher when parts.Length == 3
                => parts[1] == now.ToString("yyyyMMdd"),
            _ => true
        };
    }

    private static void ApplyResetIfNeeded(CodeSequence sequence, CodeResetType resetType)
    {
        if (resetType == CodeResetType.None || !sequence.LastResetDate.HasValue)
            return;

        var now = DateTime.UtcNow;
        var last = sequence.LastResetDate.Value;

        var shouldReset = resetType switch
        {
            CodeResetType.Daily   => last.Date != now.Date,
            CodeResetType.Monthly => last.Year != now.Year || last.Month != now.Month,
            CodeResetType.Yearly  => last.Year != now.Year,
            _ => false
        };

        if (shouldReset)
        {
            sequence.LastNumber = 0;
            sequence.LastResetDate = now;
        }
    }

    private static string FormatCode(string moduleName, ModuleConfig config, long number)
    {
        var padded = number.ToString($"D{config.PadLength}");
        var now = DateTime.UtcNow;

        return moduleName switch
        {
            CodeModuleNames.Purchase         => $"{config.Prefix}-{now:yyyyMM}-{padded}",
            CodeModuleNames.SalesInvoice     => $"{config.Prefix}-{now:yyyyMMdd}-{padded}",
            CodeModuleNames.CustomerReceipt  => $"{config.Prefix}-{now:yyyyMMdd}-{padded}",
            CodeModuleNames.SupplierPayment  => $"{config.Prefix}-{now:yyyyMMdd}-{padded}",
            CodeModuleNames.Expense          => $"{config.Prefix}-{now:yyyyMMdd}-{padded}",
            CodeModuleNames.JournalVoucher   => $"{config.Prefix}-{now:yyyyMMdd}-{padded}",
            _                              => $"{config.Prefix}-{padded}"
        };
    }

    private static string GenerateEan13()
    {
        var digits = new int[12];
        digits[0] = 2;
        digits[1] = 0;

        for (var i = 2; i < 12; i++)
            digits[i] = Random.Shared.Next(0, 10);

        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += digits[i] * (i % 2 == 0 ? 1 : 3);

        var checkDigit = (10 - (sum % 10)) % 10;
        return string.Concat(digits.Select(d => d.ToString())) + checkDigit;
    }
}
