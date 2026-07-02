using POSSystem.Application.Accounting.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Accounting.Interfaces;

public interface IGlAccountRepository
{
    Task<GlAccount?> GetByNameAsync(string name);
    Task<bool> NameExistsAsync(string name);
    Task AddAsync(GlAccount account);
    Task SaveChangesAsync();
    Task<IReadOnlyList<PartyGlLinkRow>> GetCustomersNeedingGlLinkAsync();
    Task<IReadOnlyList<PartyGlLinkRow>> GetSuppliersNeedingGlLinkAsync();
    Task SetCustomerAccountIdAsync(int customerId, int accountId);
    Task SetSupplierAccountIdAsync(int supplierId, int accountId);
    Task<BranchGlAccounts> ResolvePostingAccountsAsync();
    Task EnsureGlobalCoaHierarchyAsync();
    Task MigrateToGlobalCoaAsync();
    Task<int?> GetAccountIdByNameAsync(string name);
    Task<IReadOnlyList<int>> GetDescendantAccountIdsAsync(int rootAccountId);
    Task<int?> GetCustomerGlAccountIdAsync(int customerId, int businessId, int branchId);
    Task<int?> GetSupplierGlAccountIdAsync(int supplierId, int businessId, int branchId);
    Task<PartyGlLinkRow?> GetSupplierPartyRowAsync(int supplierId, int businessId, int branchId);
    Task<int?> GetExpenseCategoryGlAccountIdAsync(int expenseCategoryId);
    Task<IReadOnlyList<(int Id, string Name)>> GetExpenseCategoriesNeedingGlLinkAsync();
    Task SetExpenseCategoryGlAccountIdAsync(int categoryId, int accountId);
    Task UpdateGlAccountNameAsync(int accountId, string name);
}
