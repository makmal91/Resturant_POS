using POSSystem.Application.Accounting.DTOs;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Accounting.Services;
using POSSystem.Application.Ledger.DTOs;
using POSSystem.Application.Ledger.Interfaces;
using POSSystem.Application.Payments.DTOs;
using POSSystem.Application.Payments.Interfaces;
using POSSystem.Domain;
using CustomerEntity = POSSystem.Domain.Customer;
using SupplierEntity = POSSystem.Domain.Supplier;

namespace POSSystem.Application.Ledger.Services;

/// <summary>
/// Customer/supplier ledger views from GL transactions on the party sub-account.
/// </summary>
public class PartyLedgerService : IPartyLedgerService
{
    private readonly IPartyLedgerRepository _repository;
    private readonly IInvoicePaymentService _invoicePaymentService;
    private readonly IAccountLedgerService _accountLedger;
    private readonly IGlAccountRepository _glAccounts;

    public PartyLedgerService(
        IPartyLedgerRepository repository,
        IInvoicePaymentService invoicePaymentService,
        IAccountLedgerService accountLedger,
        IGlAccountRepository glAccounts)
    {
        _repository = repository;
        _invoicePaymentService = invoicePaymentService;
        _accountLedger = accountLedger;
        _glAccounts = glAccounts;
    }

    public async Task<PartyLedgerEntryDto> ReceiveCustomerPaymentAsync(ReceiveCustomerPaymentDto dto)
    {
        var payment = await _invoicePaymentService.RecordCustomerPaymentAsync(new RecordCustomerPaymentDto
        {
            CustomerId = dto.CustomerId,
            SaleInvoiceId = dto.SaleInvoiceId,
            PaymentType = dto.PaymentType,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            ReferenceNo = dto.ReferenceNo,
            Notes = dto.Notes,
            AutoAllocate = dto.AutoAllocate,
            Allocations = dto.Allocations,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        });

        return MapPaymentToLedgerEntry(payment, isCustomer: true);
    }

    public async Task<PartyLedgerEntryDto> PaySupplierAsync(PaySupplierDto dto)
    {
        var payment = await _invoicePaymentService.RecordSupplierPaymentAsync(new RecordSupplierPaymentDto
        {
            SupplierId = dto.SupplierId,
            PurchaseId = dto.PurchaseId,
            PaymentType = dto.PaymentType,
            Category = dto.Category,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            ReferenceNo = dto.ReferenceNo,
            Notes = dto.Notes,
            AutoAllocate = dto.AutoAllocate,
            Allocations = dto.Allocations,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        });

        return MapPaymentToLedgerEntry(payment, isCustomer: false);
    }

    public async Task<PartyBalanceDto> GetCustomerBalanceAsync(int customerId, int businessId, int branchId)
    {
        var customer = await _repository.GetCustomerAsync(customerId, businessId, branchId)
            ?? throw new InvalidOperationException("Customer not found.");

        var balance = await ResolveCustomerBalanceAsync(customer, businessId, branchId);

        return new PartyBalanceDto
        {
            PartyId = customerId,
            PartyName = customer.Name,
            Balance = balance
        };
    }

    public async Task<PartyBalanceDto> GetSupplierBalanceAsync(int supplierId, int businessId, int branchId)
    {
        var supplier = await _repository.GetSupplierAsync(supplierId, businessId, branchId)
            ?? throw new InvalidOperationException("Supplier not found.");

        var balance = await ResolveSupplierBalanceAsync(supplier, businessId, branchId);

        return new PartyBalanceDto
        {
            PartyId = supplierId,
            PartyName = supplier.Name,
            Balance = balance
        };
    }

    public Task<PartyLedgerPageDto> GetCustomerLedgerAsync(PartyLedgerFilterDto filter) =>
        GetPartyLedgerAsync(filter, isCustomer: true);

    public Task<PartyLedgerPageDto> GetSupplierLedgerAsync(PartyLedgerFilterDto filter) =>
        GetPartyLedgerAsync(filter, isCustomer: false);

    private async Task<PartyLedgerPageDto> GetPartyLedgerAsync(PartyLedgerFilterDto filter, bool isCustomer)
    {
        int accountId;
        string partyName;
        decimal currentBalance;

        if (isCustomer)
        {
            var customer = await _repository.GetCustomerAsync(filter.PartyId, filter.BusinessId, filter.BranchId)
                ?? throw new InvalidOperationException("Customer not found.");

            partyName = customer.Name;
            accountId = await ResolvePartyAccountIdAsync(
                customer.AccountId,
                () => _glAccounts.GetCustomerGlAccountIdAsync(customer.Id, filter.BusinessId, filter.BranchId));
            currentBalance = await ResolveCustomerBalanceAsync(customer, filter.BusinessId, filter.BranchId);
        }
        else
        {
            var supplier = await _repository.GetSupplierAsync(filter.PartyId, filter.BusinessId, filter.BranchId)
                ?? throw new InvalidOperationException("Supplier not found.");

            partyName = supplier.Name;
            accountId = await ResolvePartyAccountIdAsync(
                supplier.AccountId,
                () => _glAccounts.GetSupplierGlAccountIdAsync(supplier.Id, filter.BusinessId, filter.BranchId));
            currentBalance = await ResolveSupplierBalanceAsync(supplier, filter.BusinessId, filter.BranchId);
        }

        if (accountId <= 0)
        {
            throw new InvalidOperationException(
                isCustomer ? "Customer GL account is not configured." : "Supplier GL account is not configured.");
        }

        var ledger = await _accountLedger.GetAccountLedgerAsync(new AccountLedgerFilterDto
        {
            AccountId = accountId,
            BusinessId = filter.BusinessId,
            BranchId = filter.BranchId,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            Page = filter.Page,
            PageSize = filter.PageSize,
            AuditView = filter.AuditView,
            GroupByChain = filter.GroupByChain,
        });

        var page = AccountLedgerMapper.ToPartyLedgerPage(ledger, filter.PartyId, partyName, filter.FromDate);
        page.CurrentBalance = currentBalance;
        return page;
    }

    private static async Task<int> ResolvePartyAccountIdAsync(int? linkedAccountId, Func<Task<int?>> lookup)
    {
        if (linkedAccountId is > 0)
            return linkedAccountId.Value;

        return await lookup() ?? 0;
    }

    private async Task<decimal> ResolveCustomerBalanceAsync(CustomerEntity customer, int businessId, int branchId)
    {
        if (customer.AccountId is > 0)
            return await _accountLedger.GetDisplayBalanceAsync(customer.AccountId.Value, businessId, branchId);

        var accountId = await _glAccounts.GetCustomerGlAccountIdAsync(customer.Id, businessId, branchId);
        if (accountId is > 0)
            return await _accountLedger.GetDisplayBalanceAsync(accountId.Value, businessId, branchId);

        return 0m;
    }

    private async Task<decimal> ResolveSupplierBalanceAsync(SupplierEntity supplier, int businessId, int branchId)
    {
        if (supplier.AccountId is > 0)
            return await _accountLedger.GetDisplayBalanceAsync(supplier.AccountId.Value, businessId, branchId);

        var accountId = await _glAccounts.GetSupplierGlAccountIdAsync(supplier.Id, businessId, branchId);
        if (accountId is > 0)
            return await _accountLedger.GetDisplayBalanceAsync(accountId.Value, businessId, branchId);

        return 0m;
    }

    private static PartyLedgerEntryDto MapPaymentToLedgerEntry(InvoicePaymentDto payment, bool isCustomer)
    {
        var description = payment.Notes;
        if (payment.InvoiceId.HasValue && !string.IsNullOrWhiteSpace(payment.InvoiceNo))
            description = $"Invoice: {payment.InvoiceNo}" + (string.IsNullOrWhiteSpace(payment.Notes) ? "" : $" | {payment.Notes}");

        return new PartyLedgerEntryDto
        {
            Id = payment.Id,
            Date = payment.PaymentDate,
            Type = isCustomer ? CustomerLedgerTransactionType.PaymentReceived.ToString() : SupplierLedgerTransactionType.PaymentMade.ToString(),
            Description = description,
            Debit = isCustomer ? 0 : payment.Amount,
            Credit = isCustomer ? payment.Amount : 0,
            RunningBalance = 0,
            ReferenceId = payment.InvoiceId ?? payment.Id,
            PaymentId = payment.Id,
        };
    }
}
