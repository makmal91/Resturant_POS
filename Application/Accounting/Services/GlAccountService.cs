using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Accounting.Services;

public class GlAccountService : IGlAccountService
{
    private readonly IGlAccountRepository _repository;

    public GlAccountService(IGlAccountRepository repository) => _repository = repository;

    public Task<int> CreateCustomerReceivableAccountAsync(
        int businessId, int branchId, string customerName, string customerCode) =>
        CreatePartySubAccountAsync(
            GlAccountDefaults.CustomerPartyParent,
            AccountType.Asset,
            customerName,
            customerCode);

    public Task<int> CreateSupplierPayableAccountAsync(
        int businessId, int branchId, string supplierName, string supplierCode) =>
        CreatePartySubAccountAsync(
            GlAccountDefaults.SupplierPartyParent,
            AccountType.Liability,
            supplierName,
            supplierCode);

    public async Task BackfillPartyAccountLinksAsync()
    {
        foreach (var customer in await _repository.GetCustomersNeedingGlLinkAsync())
        {
            var accountId = await CreateCustomerReceivableAccountAsync(
                customer.BusinessId, customer.BranchId, customer.Name, customer.Code);
            await _repository.SetCustomerAccountIdAsync(customer.PartyId, accountId);
        }

        foreach (var supplier in await _repository.GetSuppliersNeedingGlLinkAsync())
        {
            var accountId = await CreateSupplierPayableAccountAsync(
                supplier.BusinessId, supplier.BranchId, supplier.Name, supplier.Code);
            await _repository.SetSupplierAccountIdAsync(supplier.PartyId, accountId);
        }

        await _repository.SaveChangesAsync();
    }

    public async Task BackfillExpenseCategoryGlLinksAsync()
    {
        foreach (var category in await _repository.GetExpenseCategoriesNeedingGlLinkAsync())
            await EnsureExpenseCategoryGlAccountAsync(category.Id, category.Name);
    }

    public async Task<int> EnsureSupplierPayableAccountLinkedAsync(int supplierId, int businessId, int branchId)
    {
        var existing = await _repository.GetSupplierGlAccountIdAsync(supplierId, businessId, branchId);
        if (existing is > 0)
            return existing.Value;

        var supplier = await _repository.GetSupplierPartyRowAsync(supplierId, businessId, branchId)
            ?? throw new InvalidOperationException("Supplier not found.");

        var accountId = await CreateSupplierPayableAccountAsync(
            supplier.BusinessId, supplier.BranchId, supplier.Name, supplier.Code);
        await _repository.SetSupplierAccountIdAsync(supplierId, accountId);
        await _repository.SaveChangesAsync();
        return accountId;
    }

    public async Task<int> EnsureExpenseCategoryGlAccountAsync(int categoryId, string categoryName)
    {
        var existing = await _repository.GetExpenseCategoryGlAccountIdAsync(categoryId);
        if (existing is > 0)
            return existing.Value;

        var accountId = await CreateExpenseCategorySubAccountAsync(categoryName);
        await _repository.SetExpenseCategoryGlAccountIdAsync(categoryId, accountId);
        await _repository.SaveChangesAsync();
        return accountId;
    }

    public async Task SyncExpenseCategoryGlAccountNameAsync(int glAccountId, string categoryName)
    {
        var trimmed = categoryName.Trim();
        if (glAccountId <= 0 || string.IsNullOrWhiteSpace(trimmed))
            return;

        await _repository.UpdateGlAccountNameAsync(glAccountId, trimmed);
        await _repository.SaveChangesAsync();
    }

    private async Task<int> CreateExpenseCategorySubAccountAsync(string categoryName)
    {
        await _repository.EnsureGlobalCoaHierarchyAsync();

        var parent = await _repository.GetByNameAsync(GlAccountDefaults.GeneralExpense)
            ?? throw new InvalidOperationException(
                $"GL parent account '{GlAccountDefaults.GeneralExpense}' was not found. Run database seed to create the default chart of accounts.");

        var accountName = categoryName.Trim();
        if (accountName.Length > 200)
            accountName = accountName[..200];

        var existing = await _repository.GetByNameAsync(accountName);
        if (existing != null)
            return existing.Id;

        var account = new GlAccount
        {
            Name = accountName,
            Type = AccountType.Expense,
            ParentId = parent.Id,
            IsActive = true,
        };

        await _repository.AddAsync(account);
        await _repository.SaveChangesAsync();
        return account.Id;
    }

    private async Task<int> CreatePartySubAccountAsync(
        string parentAccountName,
        AccountType type,
        string partyName,
        string partyCode)
    {
        await _repository.EnsureGlobalCoaHierarchyAsync();

        var parent = await _repository.GetByNameAsync(parentAccountName)
            ?? throw new InvalidOperationException(
                $"GL parent account '{parentAccountName}' was not found. Run database seed to create the default chart of accounts.");

        var accountName = GlAccountDefaults.FormatPartyAccountName(partyName, partyCode);
        var existing = await _repository.GetByNameAsync(accountName);
        if (existing != null)
            return existing.Id;

        var account = new GlAccount
        {
            Name = accountName,
            Type = type,
            ParentId = parent.Id,
            IsActive = true,
        };

        await _repository.AddAsync(account);
        await _repository.SaveChangesAsync();
        return account.Id;
    }
}
