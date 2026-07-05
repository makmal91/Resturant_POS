using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Stock.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Services;

/// <summary>
/// Repairs opening-stock data issues: voucher/product ReferenceId collisions, duplicate GL, duplicate product-form stock.
/// </summary>
public class OpeningStockDuplicateCleanupService
{
    private readonly POSDbContext _db;
    private readonly IStockLedgerRepository _stockLedger;
    private readonly IAccountingIntegrationService _accountingIntegration;
    private readonly IAccountingRepository _accountingRepository;
    private readonly ILogger<OpeningStockDuplicateCleanupService> _logger;

    public OpeningStockDuplicateCleanupService(
        POSDbContext db,
        IStockLedgerRepository stockLedger,
        IAccountingIntegrationService accountingIntegration,
        IAccountingRepository accountingRepository,
        ILogger<OpeningStockDuplicateCleanupService> logger)
    {
        _db = db;
        _stockLedger = stockLedger;
        _accountingIntegration = accountingIntegration;
        _accountingRepository = accountingRepository;
        _logger = logger;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        await RepairVoucherEditCollisionsAsync(cancellationToken);

        var activeVoucherIds = await _db.OpeningStockVouchers
            .AsNoTracking()
            .Where(v => !v.IsDeleted && !v.IsReversed)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        if (activeVoucherIds.Count == 0)
            return;

        var voucherLineKeys = await _db.OpeningStockVoucherLines
            .AsNoTracking()
            .Where(l => !l.IsDeleted && activeVoucherIds.Contains(l.VoucherId))
            .Select(l => new ProductVariantBranchKey(l.ProductId, l.VariantId, l.BusinessId, l.BranchId))
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var key in voucherLineKeys)
        {
            try
            {
                await CleanupKeyAsync(key, activeVoucherIds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Skipped duplicate opening stock cleanup for product {ProductId} variant {VariantId}.",
                    key.ProductId,
                    key.VariantId);
            }
        }
    }

    /// <summary>
    /// Voucher edit used ReferenceId = voucher.Id, which collides with product-form opening (ReferenceId = product.Id).
    /// Restores product-form stock that was wrongly reversed when product id matched voucher id.
    /// </summary>
    private async Task RepairVoucherEditCollisionsAsync(CancellationToken cancellationToken)
    {
        var vouchers = await _db.OpeningStockVouchers
            .AsNoTracking()
            .Include(v => v.Lines.Where(l => !l.IsDeleted))
            .Where(v => !v.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var voucher in vouchers)
        {
            var lineKeys = voucher.Lines
                .Select(l => (l.ProductId, l.VariantId))
                .ToHashSet();

            var wrongfulReversals = await _db.StockLedgerEntries
                .Where(e => !e.IsDeleted
                            && e.Type == StockLedgerType.OpeningReversal
                            && e.ReferenceId == voucher.Id
                            && e.BusinessId == voucher.BusinessId
                            && e.BranchId == voucher.BranchId)
                .ToListAsync(cancellationToken);

            foreach (var reversal in wrongfulReversals)
            {
                if (lineKeys.Contains((reversal.ProductId, reversal.VariantId)))
                    continue;

                var hasProductFormOpening = await _db.StockLedgerEntries
                    .AsNoTracking()
                    .AnyAsync(
                        e => !e.IsDeleted
                             && e.ProductId == reversal.ProductId
                             && e.VariantId == reversal.VariantId
                             && e.BusinessId == reversal.BusinessId
                             && e.BranchId == reversal.BranchId
                             && e.Type == StockLedgerType.Opening
                             && e.ReferenceId == reversal.ProductId
                             && e.Remarks.Contains("Product:"),
                        cancellationToken);

                if (!hasProductFormOpening)
                    continue;

                var alreadyRepaired = await _db.StockLedgerEntries
                    .AsNoTracking()
                    .AnyAsync(
                        e => !e.IsDeleted
                             && e.ProductId == reversal.ProductId
                             && e.VariantId == reversal.VariantId
                             && e.BusinessId == reversal.BusinessId
                             && e.BranchId == reversal.BranchId
                             && e.Type == StockLedgerType.Opening
                             && e.ReferenceId == reversal.ProductId
                             && e.Remarks.Contains("Repair — restored product opening"),
                        cancellationToken);

                if (alreadyRepaired)
                    continue;

                try
                {
                    await _stockLedger.RunInSerializableTransactionAsync(async () =>
                    {
                        await _stockLedger.AddAsync(new StockLedger
                        {
                            ProductId = reversal.ProductId,
                            VariantId = reversal.VariantId,
                            WarehouseId = reversal.WarehouseId,
                            Type = StockLedgerType.Opening,
                            ReferenceId = reversal.ProductId,
                            QuantityInBaseUnit = -reversal.QuantityInBaseUnit,
                            UnitId = reversal.UnitId,
                            UnitQuantity = reversal.UnitQuantity.HasValue
                                ? -reversal.UnitQuantity.Value
                                : null,
                            UnitPrice = reversal.UnitPrice,
                            TotalAmount = reversal.TotalAmount,
                            Date = DateTime.UtcNow,
                            Remarks =
                                $"Repair — restored product opening reversed by voucher edit collision ({voucher.VoucherNo})",
                            BusinessId = reversal.BusinessId,
                            BranchId = reversal.BranchId
                        });
                        await _stockLedger.SaveChangesAsync();
                    });

                    _logger.LogInformation(
                        "Restored product-form opening stock for product {ProductId} after voucher {VoucherNo} edit collision.",
                        reversal.ProductId,
                        voucher.VoucherNo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to restore product opening for product {ProductId} (voucher {VoucherNo}).",
                        reversal.ProductId,
                        voucher.VoucherNo);
                }
            }
        }
    }

    private async Task CleanupKeyAsync(
        ProductVariantBranchKey key,
        IReadOnlyList<int> activeVoucherIds,
        CancellationToken cancellationToken)
    {
        var hasVoucherOpening = await _db.StockLedgerEntries
            .AsNoTracking()
            .AnyAsync(
                e => !e.IsDeleted
                     && e.ProductId == key.ProductId
                     && e.VariantId == key.VariantId
                     && e.BusinessId == key.BusinessId
                     && e.BranchId == key.BranchId
                     && e.Type == StockLedgerType.Opening
                     && e.ReferenceId.HasValue
                     && activeVoucherIds.Contains(e.ReferenceId.Value),
                cancellationToken);

        if (!hasVoucherOpening)
            return;

        var hasDuplicateGl = await _accountingRepository.ExistsForReferenceAsync(
            key.ProductId, GlTransactionType.OpeningBalance);

        var netProductFormQty = await _db.StockLedgerEntries
            .AsNoTracking()
            .Where(e => !e.IsDeleted
                        && e.ProductId == key.ProductId
                        && e.VariantId == key.VariantId
                        && e.BusinessId == key.BusinessId
                        && e.BranchId == key.BranchId
                        && (e.Type == StockLedgerType.Opening || e.Type == StockLedgerType.OpeningReversal)
                        && e.ReferenceId == key.ProductId)
            .SumAsync(e => (decimal?)e.QuantityInBaseUnit, cancellationToken)
            .ConfigureAwait(false) ?? 0m;

        if (netProductFormQty <= 0.0001m && !hasDuplicateGl)
            return;

        await _stockLedger.RunInSerializableTransactionAsync(async () =>
        {
            if (hasDuplicateGl)
            {
                await _accountingIntegration.ReverseTransactionAsync(
                    key.ProductId,
                    GlTransactionType.OpeningBalance,
                    "Duplicate opening stock — voucher takes precedence");
            }

            if (netProductFormQty > 0.0001m)
                await ReverseProductFormOpeningStockAsync(key);

            await _stockLedger.SaveChangesAsync();
        });

        _logger.LogInformation(
            "Cleaned duplicate opening stock for product {ProductId} variant {VariantId} (branch {BranchId}).",
            key.ProductId,
            key.VariantId,
            key.BranchId);
    }

    private async Task ReverseProductFormOpeningStockAsync(ProductVariantBranchKey key)
    {
        var entries = await _db.StockLedgerEntries
            .Where(e => !e.IsDeleted
                        && e.ProductId == key.ProductId
                        && e.VariantId == key.VariantId
                        && e.BusinessId == key.BusinessId
                        && e.BranchId == key.BranchId
                        && (e.Type == StockLedgerType.Opening || e.Type == StockLedgerType.OpeningReversal)
                        && e.ReferenceId == key.ProductId)
            .ToListAsync();

        if (entries.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var groups = entries.GroupBy(e => e.WarehouseId);

        foreach (var group in groups)
        {
            var netQty = group.Sum(e => e.QuantityInBaseUnit);
            if (netQty <= 0.0001m)
                continue;

            var netAmount = group.Sum(e =>
                e.Type == StockLedgerType.Opening ? e.TotalAmount : -e.TotalAmount);
            if (netAmount <= 0)
                continue;

            var template = group
                .Where(e => e.Type == StockLedgerType.Opening)
                .OrderByDescending(e => e.Id)
                .First();

            decimal? unitQty = null;
            if (template.UnitQuantity.HasValue && template.QuantityInBaseUnit != 0)
            {
                var factor = template.QuantityInBaseUnit / template.UnitQuantity.Value;
                unitQty = factor != 0 ? -(netQty / factor) : -template.UnitQuantity.Value;
            }

            await _stockLedger.AddAsync(new StockLedger
            {
                ProductId = key.ProductId,
                VariantId = key.VariantId,
                WarehouseId = group.Key,
                Type = StockLedgerType.OpeningReversal,
                ReferenceId = key.ProductId,
                QuantityInBaseUnit = -netQty,
                UnitId = template.UnitId,
                UnitQuantity = unitQty,
                UnitPrice = template.UnitPrice,
                TotalAmount = netAmount,
                Date = now,
                Remarks = "Duplicate opening stock removed — voucher takes precedence",
                BusinessId = key.BusinessId,
                BranchId = key.BranchId
            });
        }
    }

    private readonly record struct ProductVariantBranchKey(int ProductId, int? VariantId, int BusinessId, int BranchId);
}
