using POSSystem.Application.Ledger.DTOs;

namespace POSSystem.Application.Ledger.Interfaces;

public interface IPartyLedgerService
{
    Task<PartyLedgerEntryDto> ReceiveCustomerPaymentAsync(ReceiveCustomerPaymentDto dto);
    Task<PartyLedgerEntryDto> PaySupplierAsync(PaySupplierDto dto);
    Task<PartyBalanceDto> GetCustomerBalanceAsync(int customerId, int businessId, int branchId);
    Task<PartyBalanceDto> GetSupplierBalanceAsync(int supplierId, int businessId, int branchId);
    Task<PartyLedgerPageDto> GetCustomerLedgerAsync(PartyLedgerFilterDto filter);
    Task<PartyLedgerPageDto> GetSupplierLedgerAsync(PartyLedgerFilterDto filter);
}
