using POSSystem.Application.Ledger.DTOs;
using CustomerEntity = POSSystem.Domain.Customer;using SupplierEntity = POSSystem.Domain.Supplier;
using POSSystem.Domain;

namespace POSSystem.Application.Ledger.Interfaces;

public interface IPartyLedgerRepository
{
    Task<CustomerLedgerTransaction> AddCustomerEntryAsync(CustomerLedgerTransaction entry);
    Task<SupplierLedgerTransaction> AddSupplierEntryAsync(SupplierLedgerTransaction entry);
    Task<decimal> GetCustomerRunningBalanceAsync(int customerId, int businessId, int branchId);
    Task<decimal> GetSupplierRunningBalanceAsync(int supplierId, int businessId, int branchId);
    Task<List<CustomerLedgerTransaction>> GetCustomerEntriesByReferenceAsync(
        int referenceId, int businessId, int branchId, CustomerLedgerTransactionType type);
    Task<List<SupplierLedgerTransaction>> GetSupplierEntriesByReferenceAsync(
        int referenceId, int businessId, int branchId, SupplierLedgerTransactionType type);
    Task<CustomerEntity?> GetCustomerAsync(int customerId, int businessId, int branchId);
    Task<SupplierEntity?> GetSupplierAsync(int supplierId, int businessId, int branchId);
    Task<PartyLedgerPageDto> GetCustomerLedgerPagedAsync(PartyLedgerFilterDto filter);
    Task<PartyLedgerPageDto> GetSupplierLedgerPagedAsync(PartyLedgerFilterDto filter);
    Task SaveChangesAsync();
}
