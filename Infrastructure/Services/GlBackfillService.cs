using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Services;

/// <summary>
/// Idempotent backfill of GL journals for business documents missing active Transactions rows.
/// </summary>
public class GlBackfillService
{
    private readonly POSDbContext _db;
    private readonly IAccountingRepository _accountingRepository;
    private readonly IAccountingIntegrationService _integration;
    private readonly IGlAccountService _glAccounts;
    private readonly ILogger<GlBackfillService> _logger;

    public GlBackfillService(
        POSDbContext db,
        IAccountingRepository accountingRepository,
        IAccountingIntegrationService integration,
        IGlAccountService glAccounts,
        ILogger<GlBackfillService> logger)
    {
        _db = db;
        _accountingRepository = accountingRepository;
        _integration = integration;
        _glAccounts = glAccounts;
        _logger = logger;
    }

    public async Task BackfillMissingJournalsAsync(CancellationToken cancellationToken = default)
    {
        await _glAccounts.BackfillPartyAccountLinksAsync();
        await _glAccounts.BackfillExpenseCategoryGlLinksAsync();

        var stockEnabled = await _db.PermissionModules.AsNoTracking()
            .AnyAsync(m => m.ModuleKey == PermissionModules.Stock && m.IsActive && !m.IsDeleted, cancellationToken);

        var purchases = await _db.Purchases.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status == PurchaseStatus.Posted)
            .ToListAsync(cancellationToken);

        foreach (var purchase in purchases)
        {
            if (await _accountingRepository.HasCompleteBalancedJournalAsync(purchase.Id, GlTransactionType.Purchase))
                continue;

            try
            {
                await _integration.PostPurchaseAsync(purchase, stockEnabled);
                _logger.LogInformation("Backfilled GL purchase journal for purchase {PurchaseId}.", purchase.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipped GL backfill for purchase {PurchaseId}.", purchase.Id);
            }
        }

        var sales = await _db.SaleInvoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Where(i => !i.IsDeleted && i.Status == SaleInvoiceStatus.Completed)
            .ToListAsync(cancellationToken);

        foreach (var invoice in sales)
        {
            if (await _accountingRepository.ExistsForReferenceAsync(invoice.Id, GlTransactionType.Sale))
                continue;

            try
            {
                await _integration.PostSaleAsync(invoice, stockEnabled);
                _logger.LogInformation("Backfilled GL sale journal for invoice {InvoiceId}.", invoice.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipped GL backfill for sale {InvoiceId}.", invoice.Id);
            }
        }

        var expenses = await _db.Expenses.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var expense in expenses)
        {
            if (await _accountingRepository.ExistsForReferenceAsync(expense.Id, GlTransactionType.Expense))
                continue;

            try
            {
                await _integration.PostExpenseAsync(expense);
                _logger.LogInformation("Backfilled GL expense journal for expense {ExpenseId}.", expense.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipped GL backfill for expense {ExpenseId}.", expense.Id);
            }
        }

        var payments = await _db.InvoicePayments.AsNoTracking()
            .Where(p => !p.IsDeleted && !p.IsReversed)
            .ToListAsync(cancellationToken);

        foreach (var payment in payments)
        {
            // POS at-sale tender is already booked by the sale journal; never post a receipt for it.
            if (payment.Category == InvoicePaymentCategory.PosSale)
                continue;

            var type = payment.Module == InvoicePaymentModule.Sale
                ? GlTransactionType.Receipt
                : GlTransactionType.Payment;

            if (await _accountingRepository.HasCompleteBalancedJournalAsync(payment.Id, type))
                continue;

            try
            {
                if (type == GlTransactionType.Receipt)
                    await _integration.PostPaymentReceivedAsync(payment);
                else
                    await _integration.PostPaymentPaidAsync(payment);

                _logger.LogInformation("Backfilled GL payment journal for payment {PaymentId}.", payment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipped GL backfill for payment {PaymentId}.", payment.Id);
            }
        }

        var openingStockProducts = await _db.StockLedgerEntries
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.Type == StockLedgerType.Opening)
            .Select(e => new { e.ProductId, e.BusinessId, e.BranchId })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var row in openingStockProducts)
        {
            if (await _accountingRepository.ExistsForReferenceAsync(row.ProductId, GlTransactionType.OpeningBalance))
                continue;

            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == row.ProductId && p.BusinessId == row.BusinessId && p.BranchId == row.BranchId && !p.IsDeleted,
                    cancellationToken);

            if (product == null)
                continue;

            var openingEntries = await _db.StockLedgerEntries
                .AsNoTracking()
                .Where(e => !e.IsDeleted
                            && e.ProductId == row.ProductId
                            && e.BusinessId == row.BusinessId
                            && e.BranchId == row.BranchId
                            && e.Type == StockLedgerType.Opening)
                .ToListAsync(cancellationToken);

            var amount = Math.Round(openingEntries.Sum(e => e.TotalAmount), 2, MidpointRounding.AwayFromZero);
            if (amount <= 0)
                continue;

            try
            {
                await _integration.PostOpeningStockAsync(product, amount, row.BusinessId, row.BranchId);
                _logger.LogInformation("Backfilled GL opening stock journal for product {ProductId}.", product.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipped GL backfill for opening stock on product {ProductId}.", product.Id);
            }
        }
    }
}
