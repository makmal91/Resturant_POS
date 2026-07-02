using POSSystem.Domain;
using PurchaseEntity = POSSystem.Domain.Purchase;
using ExpenseEntity = POSSystem.Domain.Expense;

namespace POSSystem.Application.Accounting.Interfaces;

public interface IAccountingIntegrationService
{
    Task PostSaleAsync(SaleInvoice invoice, bool stockModuleEnabled);
    Task PostPurchaseAsync(PurchaseEntity purchase, bool stockModuleEnabled);
    Task PostPaymentReceivedAsync(InvoicePayment payment);
    Task PostPaymentPaidAsync(InvoicePayment payment);
    Task PostExpenseAsync(ExpenseEntity expense);
    Task ReverseTransactionAsync(int referenceId, GlTransactionType transactionType, string? reason = null);
}
