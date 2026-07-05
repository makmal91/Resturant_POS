using POSSystem.Domain;
using PurchaseEntity = POSSystem.Domain.Purchase;
using ExpenseEntity = POSSystem.Domain.Expense;
using ProductEntity = POSSystem.Domain.Product;
using StockAdjustmentEntity = POSSystem.Domain.StockAdjustment;

namespace POSSystem.Application.Accounting.Interfaces;

public interface IAccountingIntegrationService
{
    Task PostSaleAsync(SaleInvoice invoice, bool stockModuleEnabled);
    Task PostPurchaseAsync(PurchaseEntity purchase, bool stockModuleEnabled);
    Task PostPaymentReceivedAsync(InvoicePayment payment);
    Task PostPaymentPaidAsync(InvoicePayment payment);
    Task PostExpenseAsync(ExpenseEntity expense);
    Task PostOpeningStockAsync(ProductEntity product, decimal amount, int businessId, int branchId);
    Task PostOpeningStockVoucherAsync(OpeningStockVoucher voucher, decimal amount);
    Task PostStockAdjustmentAsync(StockAdjustmentEntity adjustment, AdjustmentType adjustmentType, decimal gainAmount, decimal lossAmount);
    Task ReverseTransactionAsync(int referenceId, GlTransactionType transactionType, string? reason = null);
}
