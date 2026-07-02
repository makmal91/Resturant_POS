using POSSystem.Application.Ledger.DTOs;
using CustomerEntity = POSSystem.Domain.Customer;
using SupplierEntity = POSSystem.Domain.Supplier;

namespace POSSystem.Application.Ledger.Interfaces;

public interface IPartyLedgerRepository
{
    Task<CustomerEntity?> GetCustomerAsync(int customerId, int businessId, int branchId);
    Task<SupplierEntity?> GetSupplierAsync(int supplierId, int businessId, int branchId);
    Task<List<PartyLedgerSourceDto>> GetSupplierActivityAsync(
        int supplierId, int businessId, int branchId, bool includeReversals);
    Task<List<PartyLedgerSourceDto>> GetCustomerActivityAsync(
        int customerId, int businessId, int branchId, bool includeReversals);
}
