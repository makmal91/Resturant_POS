using System.Data;
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
        [CodeModuleNames.Purchase]     = new("PUR", 4, CodeResetType.Monthly, false),
        [CodeModuleNames.SalesInvoice] = new("INV", 4, CodeResetType.Daily,   false),
    };

    public CodeGeneratorService(POSDbContext context) => _context = context;

    public Task<string> GenerateAsync(string moduleName, int? branchId = null, CancellationToken cancellationToken = default)
        => NextCodeAsync(moduleName, branchId, increment: true, cancellationToken);

    public Task<string> PreviewAsync(string moduleName, int? branchId = null, CancellationToken cancellationToken = default)
        => NextCodeAsync(moduleName, branchId, increment: false, cancellationToken);

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

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var sequence = await _context.CodeSequences
            .FirstOrDefaultAsync(
                s => s.ModuleName == moduleName && s.BranchId == branchId,
                cancellationToken);

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
            await _context.SaveChangesAsync(cancellationToken);
        }

        ApplyResetIfNeeded(sequence, config.ResetType);

        var nextNumber = sequence.LastNumber + 1;
        var formatted = FormatCode(moduleName, config, nextNumber);

        if (increment)
        {
            sequence.LastNumber = nextNumber;
            sequence.LastResetDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return formatted;
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
            CodeModuleNames.Purchase     => $"{config.Prefix}-{now:yyyyMM}-{padded}",
            CodeModuleNames.SalesInvoice => $"{config.Prefix}-{now:yyyyMMdd}-{padded}",
            _                          => $"{config.Prefix}-{padded}"
        };
    }

    private static string GenerateEan13()
    {
        // Prefix 20 = internal/store use range (avoids real manufacturer EAN conflicts)
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
