using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Product.Interfaces;
using POSSystem.Domain;
using PurchaseEntity = POSSystem.Domain.Purchase;
using ExpenseEntity = POSSystem.Domain.Expense;
using ProductEntity = POSSystem.Domain.Product;
using StockAdjustmentEntity = POSSystem.Domain.StockAdjustment;

namespace POSSystem.Application.Accounting.Services;

public class AccountingIntegrationService : IAccountingIntegrationService
{
    private readonly IAccountingService _accounting;
    private readonly IAccountingRepository _accountingRepository;
    private readonly IGlAccountRepository _glAccounts;
    private readonly IGlAccountService _glAccountService;
    private readonly IProductRepository _productRepository;

    public AccountingIntegrationService(
        IAccountingService accounting,
        IAccountingRepository accountingRepository,
        IGlAccountRepository glAccounts,
        IGlAccountService glAccountService,
        IProductRepository productRepository)
    {
        _accounting = accounting;
        _accountingRepository = accountingRepository;
        _glAccounts = glAccounts;
        _glAccountService = glAccountService;
        _productRepository = productRepository;
    }

    public async Task PostSaleAsync(SaleInvoice invoice, bool stockModuleEnabled)
    {
        if (await _accountingRepository.ExistsForReferenceAsync(invoice.Id, GlTransactionType.Sale))
            return;

        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var groupId = Guid.NewGuid();
        var ledgerChainId = await ResolveLedgerChainAsync(invoice.Id, GlTransactionType.Sale, groupId);
        var entries = new List<AccountingTransactionDto>();
        var description = $"Sale — {invoice.InvoiceNo}";
        var date = invoice.SaleDate;
        var amount = invoice.GrandTotal;
        var branchId = invoice.BranchId;

        if (invoice.IsCreditSale)
        {
            if (!invoice.CustomerId.HasValue)
                throw new InvalidOperationException("Credit sale requires CustomerId.");

            var customerAccountId = await RequireCustomerAccountAsync(invoice.CustomerId.Value, invoice.BusinessId, branchId);
            AddLine(entries, customerAccountId, branchId, amount, 0, groupId, invoice.Id, description, GlTransactionType.Sale, date, ledgerChainId);
            AddLine(entries, accounts.Sales, branchId, 0, amount, groupId, invoice.Id, description, GlTransactionType.Sale, date, ledgerChainId);
        }
        else
        {
            var revenue = amount;
            var (cash, card) = ResolveSalePaymentAmounts(invoice);
            var tendered = cash + card;

            if (tendered <= 0)
            {
                cash = revenue;
                card = 0;
            }
            else if (tendered > revenue)
            {
                (cash, card) = AllocateNetPayment(cash, card, revenue);
            }

            if (cash > 0)
                AddLine(entries, accounts.Cash, branchId, cash, 0, groupId, invoice.Id, description, GlTransactionType.Sale, date, ledgerChainId);
            if (card > 0)
                AddLine(entries, accounts.Bank, branchId, card, 0, groupId, invoice.Id, description, GlTransactionType.Sale, date, ledgerChainId);

            var recorded = cash + card;
            if (recorded < revenue)
            {
                if (!invoice.CustomerId.HasValue)
                    throw new InvalidOperationException(
                        $"Payment received ({recorded:N2}) is less than invoice total ({revenue:N2}). Select a customer or pay the full amount.");

                var customerAccountId = await RequireCustomerAccountAsync(invoice.CustomerId.Value, invoice.BusinessId, branchId);
                AddLine(entries, customerAccountId, branchId, revenue - recorded, 0, groupId, invoice.Id, description, GlTransactionType.Sale, date, ledgerChainId);
            }

            AddLine(entries, accounts.Sales, branchId, 0, revenue, groupId, invoice.Id, description, GlTransactionType.Sale, date, ledgerChainId);
        }

        if (stockModuleEnabled && accounts.CostOfGoodsSold.HasValue)
        {
            var cogs = await CalculateSaleCogsAsync(invoice);
            if (cogs > 0)
            {
                AddLine(entries, accounts.CostOfGoodsSold.Value, branchId, cogs, 0, groupId, invoice.Id, $"{description} (COGS)", GlTransactionType.Sale, date, ledgerChainId);
                AddLine(entries, accounts.Inventory, branchId, 0, cogs, groupId, invoice.Id, $"{description} (COGS)", GlTransactionType.Sale, date, ledgerChainId);
            }
        }

        await _accounting.CreateDoubleEntryAsync(entries);
    }

    public async Task PostPurchaseAsync(PurchaseEntity purchase, bool stockModuleEnabled)
    {
        if (await _accountingRepository.HasCompleteBalancedJournalAsync(purchase.Id, GlTransactionType.Purchase))
            return;

        if (await _accountingRepository.ExistsForReferenceAsync(purchase.Id, GlTransactionType.Purchase))
            await _accounting.ReverseByReferenceAsync(
                purchase.Id, GlTransactionType.Purchase, $"Repair — {purchase.InvoiceNo}");

        var amount = purchase.TotalAmount;
        if (amount <= 0)
            throw new InvalidOperationException(
                $"Purchase '{purchase.InvoiceNo}' has no amount to post to the ledger.");

        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var groupId = Guid.NewGuid();
        var ledgerChainId = await ResolveLedgerChainAsync(purchase.Id, GlTransactionType.Purchase, groupId);
        var entries = new List<AccountingTransactionDto>();
        var description = $"Purchase — {purchase.InvoiceNo}";
        var date = purchase.PurchaseDate;
        var branchId = purchase.BranchId;
        var debitAccountId = stockModuleEnabled ? accounts.Inventory : accounts.GeneralExpense;

        AddLine(entries, debitAccountId, branchId, amount, 0, groupId, purchase.Id, description, GlTransactionType.Purchase, date, ledgerChainId);

        if (purchase.IsCreditPurchase)
        {
            var supplierAccountId = await _glAccountService.EnsureSupplierPayableAccountLinkedAsync(
                purchase.SupplierId, purchase.BusinessId, branchId);
            AddLine(entries, supplierAccountId, branchId, 0, amount, groupId, purchase.Id, description, GlTransactionType.Purchase, date, ledgerChainId);
        }
        else
        {
            AddLine(entries, accounts.Cash, branchId, 0, amount, groupId, purchase.Id, description, GlTransactionType.Purchase, date, ledgerChainId);
        }

        await _accounting.CreateDoubleEntryAsync(entries);
    }

    public async Task PostPaymentReceivedAsync(InvoicePayment payment)
    {
        // POS at-sale tender: the cash/card leg is already booked by the sale journal
        // (PostSaleAsync). Posting a receipt here would double-count cash, so skip it.
        if (payment.Category == InvoicePaymentCategory.PosSale)
            return;

        if (await _accountingRepository.ExistsForReferenceAsync(payment.Id, GlTransactionType.Receipt))
            return;

        if (!payment.CustomerId.HasValue)
            throw new InvalidOperationException("Customer payment requires CustomerId.");

        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var customerAccountId = await RequireCustomerAccountAsync(payment.CustomerId.Value, payment.BusinessId, payment.BranchId);
        var cashAccountId = ResolvePaymentAccount(accounts, payment.PaymentType);
        var groupId = Guid.NewGuid();
        var ledgerChainId = await ResolveLedgerChainAsync(payment.Id, GlTransactionType.Receipt, groupId);
        var description = $"Payment received — {payment.ReferenceNo}".Trim(' ', '—');
        var entries = new List<AccountingTransactionDto>();
        var branchId = payment.BranchId;

        AddLine(entries, cashAccountId, branchId, payment.Amount, 0, groupId, payment.Id, description, GlTransactionType.Receipt, payment.PaymentDate, ledgerChainId);
        AddLine(entries, customerAccountId, branchId, 0, payment.Amount, groupId, payment.Id, description, GlTransactionType.Receipt, payment.PaymentDate, ledgerChainId);

        await _accounting.CreateDoubleEntryAsync(entries);
    }

    public async Task PostPaymentPaidAsync(InvoicePayment payment)
    {
        if (await _accountingRepository.HasCompleteBalancedJournalAsync(payment.Id, GlTransactionType.Payment))
            return;

        if (await _accountingRepository.ExistsForReferenceAsync(payment.Id, GlTransactionType.Payment))
            await _accounting.ReverseByReferenceAsync(
                payment.Id, GlTransactionType.Payment, $"Repair — {payment.ReferenceNo}".Trim(' ', '—'));

        if (!payment.SupplierId.HasValue)
            throw new InvalidOperationException("Supplier payment requires SupplierId.");

        if (payment.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var supplierAccountId = await _glAccountService.EnsureSupplierPayableAccountLinkedAsync(
            payment.SupplierId.Value, payment.BusinessId, payment.BranchId);
        var cashAccountId = ResolvePaymentAccount(accounts, payment.PaymentType);
        var groupId = Guid.NewGuid();
        var ledgerChainId = await ResolveLedgerChainAsync(payment.Id, GlTransactionType.Payment, groupId);
        var description = $"Payment paid — {payment.ReferenceNo}".Trim(' ', '—');
        var entries = new List<AccountingTransactionDto>();
        var branchId = payment.BranchId;

        AddLine(entries, supplierAccountId, branchId, payment.Amount, 0, groupId, payment.Id, description, GlTransactionType.Payment, payment.PaymentDate, ledgerChainId);
        AddLine(entries, cashAccountId, branchId, 0, payment.Amount, groupId, payment.Id, description, GlTransactionType.Payment, payment.PaymentDate, ledgerChainId);

        await _accounting.CreateDoubleEntryAsync(entries);
    }

    public async Task PostExpenseAsync(ExpenseEntity expense)
    {
        if (await _accountingRepository.ExistsForReferenceAsync(expense.Id, GlTransactionType.Expense))
            return;

        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var cashAccountId = expense.PaymentMethod == ExpensePaymentMethod.Bank
            ? accounts.Bank
            : accounts.Cash;
        var groupId = Guid.NewGuid();
        var ledgerChainId = await ResolveLedgerChainAsync(expense.Id, GlTransactionType.Expense, groupId);
        var description = $"Expense — {expense.Description}";
        var entries = new List<AccountingTransactionDto>();
        var branchId = expense.BranchId;
        var categoryAccountId = await _glAccounts.GetExpenseCategoryGlAccountIdAsync(expense.ExpenseCategoryId);
        var debitAccountId = categoryAccountId ?? accounts.GeneralExpense;

        AddLine(entries, debitAccountId, branchId, expense.Amount, 0, groupId, expense.Id, description, GlTransactionType.Expense, expense.ExpenseDate, ledgerChainId);
        AddLine(entries, cashAccountId, branchId, 0, expense.Amount, groupId, expense.Id, description, GlTransactionType.Expense, expense.ExpenseDate, ledgerChainId);

        await _accounting.CreateDoubleEntryAsync(entries);
    }

    public async Task PostOpeningStockAsync(ProductEntity product, decimal amount, int businessId, int branchId)
    {
        if (amount <= 0)
            return;

        if (await _accountingRepository.ExistsForReferenceAsync(product.Id, GlTransactionType.OpeningBalance))
            return;

        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var groupId = Guid.NewGuid();
        var ledgerChainId = groupId;
        var description = $"Opening Stock — {product.ProductName.Trim()} [{product.ProductCode.Trim()}]";
        var date = DateTime.UtcNow;
        var entries = new List<AccountingTransactionDto>();
        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        AddLine(entries, accounts.Inventory, branchId, roundedAmount, 0, groupId, product.Id, description,
            GlTransactionType.OpeningBalance, date, ledgerChainId);
        AddLine(entries, accounts.OwnerCapital, branchId, 0, roundedAmount, groupId, product.Id, description,
            GlTransactionType.OpeningBalance, date, ledgerChainId);

        await _accounting.CreateDoubleEntryAsync(entries);
    }

    public async Task PostOpeningStockVoucherAsync(OpeningStockVoucher voucher, decimal amount)
    {
        if (amount <= 0)
            return;

        if (await _accountingRepository.ExistsForReferenceAsync(voucher.Id, GlTransactionType.OpeningStockVoucher))
            return;

        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var groupId = Guid.NewGuid();
        var ledgerChainId = groupId;
        var description = $"Opening Stock — {voucher.VoucherNo}";
        if (!string.IsNullOrWhiteSpace(voucher.Description))
            description = $"{description} — {voucher.Description.Trim()}";

        var date = voucher.VoucherDate;
        var branchId = voucher.BranchId;
        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        var entries = new List<AccountingTransactionDto>();

        AddLine(entries, accounts.Inventory, branchId, roundedAmount, 0, groupId, voucher.Id, description,
            GlTransactionType.OpeningStockVoucher, date, ledgerChainId);
        AddLine(entries, accounts.OwnerCapital, branchId, 0, roundedAmount, groupId, voucher.Id, description,
            GlTransactionType.OpeningStockVoucher, date, ledgerChainId);

        await _accounting.CreateDoubleEntryAsync(entries);
    }

    public async Task PostStockAdjustmentAsync(
        StockAdjustmentEntity adjustment,
        AdjustmentType adjustmentType,
        decimal gainAmount,
        decimal lossAmount)
    {
        if (gainAmount <= 0 && lossAmount <= 0)
            return;

        if (await _accountingRepository.ExistsForReferenceAsync(
                adjustment.Id, GlTransactionType.StockAdjustmentVoucher))
            return;

        var accounts = await _glAccounts.ResolvePostingAccountsAsync();
        var groupId = Guid.NewGuid();
        var ledgerChainId = groupId;
        var description = $"Stock Adjustment — {adjustment.AdjustmentNo} — {adjustmentType.Name}";
        if (!string.IsNullOrWhiteSpace(adjustment.Remarks))
            description = $"{description} — {adjustment.Remarks.Trim()}";

        var date = adjustment.AdjustmentDate;
        var branchId = adjustment.BranchId;
        var entries = new List<AccountingTransactionDto>();

        if (lossAmount > 0)
        {
            var amt = Math.Round(lossAmount, 2, MidpointRounding.AwayFromZero);
            AddLine(entries, adjustmentType.ExpenseAccountId, branchId, amt, 0, groupId, adjustment.Id, description,
                GlTransactionType.StockAdjustmentVoucher, date, ledgerChainId);
            AddLine(entries, accounts.Inventory, branchId, 0, amt, groupId, adjustment.Id, description,
                GlTransactionType.StockAdjustmentVoucher, date, ledgerChainId);
        }

        if (gainAmount > 0)
        {
            var amt = Math.Round(gainAmount, 2, MidpointRounding.AwayFromZero);
            AddLine(entries, accounts.Inventory, branchId, amt, 0, groupId, adjustment.Id, description,
                GlTransactionType.StockAdjustmentVoucher, date, ledgerChainId);
            AddLine(entries, adjustmentType.IncomeAccountId, branchId, 0, amt, groupId, adjustment.Id, description,
                GlTransactionType.StockAdjustmentVoucher, date, ledgerChainId);
        }

        await _accounting.CreateDoubleEntryAsync(entries);
    }

    public Task ReverseTransactionAsync(int referenceId, GlTransactionType transactionType, string? reason = null) =>
        _accounting.ReverseByReferenceAsync(referenceId, transactionType, reason);

    private async Task<decimal> CalculateSaleCogsAsync(SaleInvoice invoice)
    {
        var activeItems = invoice.Items.Where(i => !i.IsDeleted).ToList();
        if (activeItems.Count == 0)
            return 0;

        var productIds = activeItems.Select(i => i.ProductId).Distinct().ToList();
        var costs = await _productRepository.GetCostPricesByIdsAsync(invoice.BusinessId, invoice.BranchId, productIds);

        decimal total = 0;
        foreach (var item in activeItems)
        {
            if (!costs.TryGetValue(item.ProductId, out var unitCost))
                continue;

            total += item.BaseQuantity * unitCost;
        }

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<int> RequireCustomerAccountAsync(int customerId, int businessId, int branchId)
    {
        var accountId = await _glAccounts.GetCustomerGlAccountIdAsync(customerId, businessId, branchId);
        if (!accountId.HasValue)
            throw new InvalidOperationException("Customer is not linked to a receivable GL account.");

        return accountId.Value;
    }

    private async Task<int> RequireSupplierAccountAsync(int supplierId, int businessId, int branchId)
    {
        var accountId = await _glAccounts.GetSupplierGlAccountIdAsync(supplierId, businessId, branchId);
        if (!accountId.HasValue)
            throw new InvalidOperationException("Supplier is not linked to a payable GL account.");

        return accountId.Value;
    }

    private static int ResolvePaymentAccount(BranchGlAccounts accounts, PartyPaymentType paymentType) =>
        paymentType == PartyPaymentType.Cash ? accounts.Cash : accounts.Bank;

    private static (decimal Cash, decimal Card) ResolveSalePaymentAmounts(SaleInvoice invoice)
    {
        var cash = invoice.CashAmount;
        var card = invoice.CardAmount;

        if (cash <= 0 && card <= 0 && invoice.PaidAmount > 0)
        {
            switch (invoice.PaymentMethod)
            {
                case SalePaymentMethod.Cash:
                    cash = invoice.PaidAmount;
                    break;
                case SalePaymentMethod.Card:
                    card = invoice.PaidAmount;
                    break;
                case SalePaymentMethod.Mixed when invoice.CashAmount > 0 || invoice.CardAmount > 0:
                    cash = invoice.CashAmount;
                    card = invoice.CardAmount;
                    break;
                case SalePaymentMethod.Mixed:
                    cash = invoice.PaidAmount;
                    break;
            }
        }

        if (cash <= 0 && card <= 0 && invoice.GrandTotal > 0)
        {
            switch (invoice.PaymentMethod)
            {
                case SalePaymentMethod.Card:
                    card = invoice.GrandTotal;
                    break;
                case SalePaymentMethod.Mixed:
                    cash = invoice.CashAmount > 0 || invoice.CardAmount > 0 ? invoice.CashAmount : invoice.GrandTotal;
                    card = invoice.CardAmount;
                    break;
                default:
                    cash = invoice.GrandTotal;
                    break;
            }
        }

        return (cash, card);
    }

    private static (decimal Cash, decimal Card) AllocateNetPayment(decimal cash, decimal card, decimal net)
    {
        var tendered = cash + card;
        if (tendered <= net)
            return (cash, card);

        if (card <= 0)
            return (net, 0);

        if (cash <= 0)
            return (0, net);

        var cardNet = Math.Min(card, net);
        return (net - cardNet, cardNet);
    }

    private async Task<Guid> ResolveLedgerChainAsync(
        int referenceId, GlTransactionType transactionType, Guid newGroupId)
    {
        var existing = await _accountingRepository.GetLedgerChainIdForReferenceAsync(referenceId, transactionType);
        return existing ?? newGroupId;
    }

    private void AddLine(
        List<AccountingTransactionDto> entries,
        int accountId,
        int branchId,
        decimal debit,
        decimal credit,
        Guid groupId,
        int referenceId,
        string description,
        GlTransactionType transactionType,
        DateTime date,
        Guid ledgerChainId)
    {
        var line = _accounting.CreateEntry(accountId, branchId, debit, credit, groupId, referenceId, description);
        line.TransactionType = transactionType;
        line.Date = date;
        line.OriginalGroupId = ledgerChainId;
        line.IsActive = true;
        entries.Add(line);
    }
}
