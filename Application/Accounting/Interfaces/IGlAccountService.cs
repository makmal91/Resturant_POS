namespace POSSystem.Application.Accounting.Interfaces;

public interface IGlAccountService
{
    Task<int> CreateCustomerReceivableAccountAsync(int businessId, int branchId, string customerName, string customerCode);
    Task<int> CreateSupplierPayableAccountAsync(int businessId, int branchId, string supplierName, string supplierCode);
    Task<int> EnsureSupplierPayableAccountLinkedAsync(int supplierId, int businessId, int branchId);
    Task<int> EnsureExpenseCategoryGlAccountAsync(int categoryId, string categoryName);
    Task SyncExpenseCategoryGlAccountNameAsync(int glAccountId, string categoryName);
    Task BackfillExpenseCategoryGlLinksAsync();
    Task BackfillPartyAccountLinksAsync();
}
