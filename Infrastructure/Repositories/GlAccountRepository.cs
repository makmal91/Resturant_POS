using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class GlAccountRepository : IGlAccountRepository
{
    private readonly POSDbContext _db;

    public GlAccountRepository(POSDbContext db) => _db = db;

    public Task<GlAccount?> GetByNameAsync(string name) =>
        _db.GlAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => !a.IsDeleted && a.Name == name);

    public async Task<int?> GetAccountIdByNameAsync(string name)
    {
        var account = await GetByNameAsync(name);
        return account?.Id;
    }

    public Task<bool> NameExistsAsync(string name) =>
        _db.GlAccounts
            .IgnoreQueryFilters()
            .AnyAsync(a => !a.IsDeleted && a.Name == name);

    public async Task AddAsync(GlAccount account) =>
        await _db.GlAccounts.AddAsync(account);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task<IReadOnlyList<PartyGlLinkRow>> GetCustomersNeedingGlLinkAsync() =>
        await _db.Customers
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.AccountId == null)
            .Select(c => new PartyGlLinkRow(c.Id, c.BusinessId, c.BranchId, c.Name, c.CustomerCode))
            .ToListAsync();

    public async Task<IReadOnlyList<PartyGlLinkRow>> GetSuppliersNeedingGlLinkAsync() =>
        await _db.Suppliers
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.AccountId == null)
            .Select(s => new PartyGlLinkRow(s.Id, s.BusinessId, s.BranchId, s.Name, s.SupplierCode))
            .ToListAsync();

    public async Task SetCustomerAccountIdAsync(int customerId, int accountId)
    {
        var customer = await _db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer != null)
            customer.AccountId = accountId;
    }

    public async Task SetSupplierAccountIdAsync(int supplierId, int accountId)
    {
        var supplier = await _db.Suppliers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == supplierId);
        if (supplier != null)
            supplier.AccountId = accountId;
    }

    public async Task<BranchGlAccounts> ResolvePostingAccountsAsync()
    {
        await EnsureGlobalCoaHierarchyAsync();

        var names = new[]
        {
            GlAccountDefaults.Cash,
            GlAccountDefaults.Bank,
            GlAccountDefaults.Inventory,
            GlAccountDefaults.OwnerCapital,
            GlAccountDefaults.OpeningStock,
            GlAccountDefaults.Sales,
            GlAccountDefaults.GeneralExpense,
            GlAccountDefaults.CostOfGoodsSold,
        };

        var accounts = await _db.GlAccounts
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive && names.Contains(a.Name))
            .Select(a => new { a.Name, a.Id })
            .ToListAsync();

        var map = accounts.ToDictionary(a => a.Name, a => a.Id, StringComparer.OrdinalIgnoreCase);

        int Require(string name) =>
            map.TryGetValue(name, out var id)
                ? id
                : throw new InvalidOperationException($"GL account '{name}' was not found. Run database seed.");

        return new BranchGlAccounts
        {
            Cash = Require(GlAccountDefaults.Cash),
            Bank = Require(GlAccountDefaults.Bank),
            Inventory = Require(GlAccountDefaults.Inventory),
            OwnerCapital = Require(GlAccountDefaults.OwnerCapital),
            OpeningStock = Require(GlAccountDefaults.OpeningStock),
            Sales = Require(GlAccountDefaults.Sales),
            GeneralExpense = Require(GlAccountDefaults.GeneralExpense),
            CostOfGoodsSold = map.TryGetValue(GlAccountDefaults.CostOfGoodsSold, out var cogs) ? cogs : null,
        };
    }

    public async Task<int?> GetCustomerGlAccountIdAsync(int customerId, int businessId, int branchId) =>
        await _db.Customers
            .IgnoreQueryFilters()
            .Where(c => c.Id == customerId && c.BusinessId == businessId && c.BranchId == branchId && !c.IsDeleted)
            .Select(c => c.AccountId)
            .FirstOrDefaultAsync();

    public async Task<int?> GetSupplierGlAccountIdAsync(int supplierId, int businessId, int branchId) =>
        await _db.Suppliers
            .IgnoreQueryFilters()
            .Where(s => s.Id == supplierId && s.BusinessId == businessId && s.BranchId == branchId && !s.IsDeleted)
            .Select(s => s.AccountId)
            .FirstOrDefaultAsync();

    public async Task<PartyGlLinkRow?> GetSupplierPartyRowAsync(int supplierId, int businessId, int branchId) =>
        await _db.Suppliers
            .IgnoreQueryFilters()
            .Where(s => s.Id == supplierId && s.BusinessId == businessId && s.BranchId == branchId && !s.IsDeleted)
            .Select(s => new PartyGlLinkRow(s.Id, s.BusinessId, s.BranchId, s.Name, s.SupplierCode))
            .FirstOrDefaultAsync();

    public async Task EnsureGlobalCoaHierarchyAsync()
    {
        var accounts = await _db.GlAccounts
            .IgnoreQueryFilters()
            .Where(a => !a.IsDeleted)
            .ToListAsync();

        var byName = accounts.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var root in GlAccountDefaults.CoaHierarchy)
            await EnsureCoaNodeAsync(root, parentId: null, byName);

        await ReparentPartyAccountsAsync(byName);
        await CleanupDuplicatePartyAccountsAsync();

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync();
    }

    public async Task MigrateToGlobalCoaAsync()
    {
        await ConsolidateDuplicateAccountsByNameAsync();
        await EnsureGlobalCoaHierarchyAsync();
    }

    public async Task<IReadOnlyList<int>> GetDescendantAccountIdsAsync(int rootAccountId)
    {
        var accounts = await _db.GlAccounts
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Select(a => new { a.Id, a.ParentId })
            .ToListAsync();

        var childrenByParent = accounts
            .Where(a => a.ParentId.HasValue)
            .GroupBy(a => a.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var result = new List<int> { rootAccountId };
        var queue = new Queue<int>();
        queue.Enqueue(rootAccountId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current, out var children))
                continue;

            foreach (var childId in children)
            {
                result.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return result;
    }

    private async Task ConsolidateDuplicateAccountsByNameAsync()
    {
        var accounts = await _db.GlAccounts.IgnoreQueryFilters().Where(a => !a.IsDeleted).ToListAsync();
        foreach (var group in accounts.GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            var canonical = group.OrderBy(a => a.Id).First();
            foreach (var duplicate in group.Skip(1))
                await RemapAccountIdAsync(duplicate.Id, canonical.Id);
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync();
    }

    private async Task RemapAccountIdAsync(int fromId, int toId)
    {
        if (fromId == toId)
            return;

        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE [dbo].[Transactions] SET [AccountId] = {0} WHERE [AccountId] = {1}", toId, fromId);

        var customers = await _db.Customers.IgnoreQueryFilters().Where(c => c.AccountId == fromId).ToListAsync();
        foreach (var customer in customers)
            customer.AccountId = toId;

        var suppliers = await _db.Suppliers.IgnoreQueryFilters().Where(s => s.AccountId == fromId).ToListAsync();
        foreach (var supplier in suppliers)
            supplier.AccountId = toId;

        var children = await _db.GlAccounts.IgnoreQueryFilters().Where(a => a.ParentId == fromId).ToListAsync();
        foreach (var child in children)
            child.ParentId = toId;

        var duplicate = await _db.GlAccounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == fromId);
        if (duplicate != null)
        {
            duplicate.IsDeleted = true;
            duplicate.IsActive = false;
            duplicate.ModifiedAt = DateTime.UtcNow;
        }
    }

    private async Task<int> EnsureCoaNodeAsync(
        GlCoaSeedNode node,
        int? parentId,
        Dictionary<string, GlAccount> byName)
    {
        if (!byName.TryGetValue(node.Name, out var account))
        {
            account = new GlAccount
            {
                Name = node.Name,
                Type = node.Type,
                ParentId = parentId,
                IsActive = true,
            };
            await _db.GlAccounts.AddAsync(account);
            await _db.SaveChangesAsync();
            byName[node.Name] = account;
        }
        else
        {
            var changed = false;
            if (account.ParentId != parentId)
            {
                account.ParentId = parentId;
                changed = true;
            }

            if (account.Type != node.Type)
            {
                account.Type = node.Type;
                changed = true;
            }

            if (changed)
                account.ModifiedAt = DateTime.UtcNow;
        }

        if (node.Children is { Length: > 0 })
        {
            foreach (var child in node.Children)
                await EnsureCoaNodeAsync(child, account.Id, byName);
        }

        return account.Id;
    }

    private async Task ReparentPartyAccountsAsync(Dictionary<string, GlAccount> byName)
    {
        if (!byName.TryGetValue(GlAccountDefaults.CustomerPartyParent, out var customersFolder)
            || !byName.TryGetValue(GlAccountDefaults.SupplierPartyParent, out var suppliersFolder))
            return;

        var customerAccountIds = await _db.Customers
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.AccountId != null)
            .Select(c => c.AccountId!.Value)
            .Distinct()
            .ToListAsync();

        foreach (var accountId in customerAccountIds)
        {
            var account = await _db.GlAccounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted);
            if (account == null)
                continue;

            account.ParentId = customersFolder.Id;
            account.Type = AccountType.Asset;
        }

        var supplierAccountIds = await _db.Suppliers
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.AccountId != null)
            .Select(s => s.AccountId!.Value)
            .Distinct()
            .ToListAsync();

        foreach (var accountId in supplierAccountIds)
        {
            var account = await _db.GlAccounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted);
            if (account == null)
                continue;

            account.ParentId = suppliersFolder.Id;
            account.Type = AccountType.Liability;
        }
    }

    private async Task CleanupDuplicatePartyAccountsAsync()
    {
        var linkedAccountIds = await _db.Customers
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.AccountId != null)
            .Select(c => c.AccountId!.Value)
            .Concat(_db.Suppliers
                .IgnoreQueryFilters()
                .Where(s => !s.IsDeleted && s.AccountId != null)
                .Select(s => s.AccountId!.Value))
            .Distinct()
            .ToListAsync();

        var linkedSet = linkedAccountIds.ToHashSet();
        var customersFolderId = await GetAccountIdByNameAsync(GlAccountDefaults.CustomerPartyParent);
        var suppliersFolderId = await GetAccountIdByNameAsync(GlAccountDefaults.SupplierPartyParent);
        if (!customersFolderId.HasValue && !suppliersFolderId.HasValue)
            return;

        var folderIds = new[] { customersFolderId, suppliersFolderId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var orphanPartyAccounts = await _db.GlAccounts
            .IgnoreQueryFilters()
            .Where(a => !a.IsDeleted
                        && a.ParentId != null
                        && folderIds.Contains(a.ParentId.Value)
                        && !linkedSet.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync();

        if (orphanPartyAccounts.Count == 0)
            return;

        var accountsWithTransactions = await _db.GlTransactions
            .AsNoTracking()
            .Where(t => t.IsActive && orphanPartyAccounts.Contains(t.AccountId))
            .Select(t => t.AccountId)
            .Distinct()
            .ToListAsync();

        var safeToRemove = orphanPartyAccounts.Except(accountsWithTransactions).ToList();
        if (safeToRemove.Count == 0)
            return;

        var toDeactivate = await _db.GlAccounts
            .IgnoreQueryFilters()
            .Where(a => safeToRemove.Contains(a.Id))
            .ToListAsync();

        foreach (var account in toDeactivate)
        {
            account.IsDeleted = true;
            account.IsActive = false;
            account.ModifiedAt = DateTime.UtcNow;
        }
    }

    public Task<int?> GetExpenseCategoryGlAccountIdAsync(int expenseCategoryId) =>
        _db.ExpenseCategories
            .IgnoreQueryFilters()
            .Where(c => c.Id == expenseCategoryId && !c.IsDeleted)
            .Select(c => c.GlAccountId)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<(int Id, string Name)>> GetExpenseCategoriesNeedingGlLinkAsync()
    {
        var list = await _db.ExpenseCategories
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.GlAccountId == null)
            .Select(c => new ValueTuple<int, string>(c.Id, c.Name))
            .ToListAsync();
        return list;
    }

    public async Task SetExpenseCategoryGlAccountIdAsync(int categoryId, int accountId)
    {
        var category = await _db.ExpenseCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);
        if (category == null)
            return;

        category.GlAccountId = accountId;
        category.ModifiedAt = DateTime.UtcNow;
    }

    public async Task UpdateGlAccountNameAsync(int accountId, string name)
    {
        var account = await _db.GlAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted);
        if (account == null)
            return;

        account.Name = name.Length <= 200 ? name : name[..200];
        account.ModifiedAt = DateTime.UtcNow;
    }
}
