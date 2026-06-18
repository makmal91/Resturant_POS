using POSSystem.Application.Reports.DTOs;
using POSSystem.Application.Reports.Interfaces;

namespace POSSystem.Application.Reports.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _repository;

    public ReportService(IReportRepository repository) => _repository = repository;

    public Task<ReportPagedResultDto<SalesReportRowDto>> GetSalesReportAsync(ReportFilterDto filter)
        => _repository.GetSalesReportAsync(filter);

    public Task<ReportPagedResultDto<PurchaseReportRowDto>> GetPurchaseReportAsync(ReportFilterDto filter)
        => _repository.GetPurchaseReportAsync(filter);

    public Task<ReportPagedResultDto<CustomerOutstandingRowDto>> GetCustomerOutstandingReportAsync(ReportFilterDto filter)
        => _repository.GetCustomerOutstandingReportAsync(filter);

    public Task<ReportPagedResultDto<SupplierPayableRowDto>> GetSupplierPayableReportAsync(ReportFilterDto filter)
        => _repository.GetSupplierPayableReportAsync(filter);

    public Task<ReportPagedResultDto<ProfitLossRowDto>> GetProfitLossReportAsync(ReportFilterDto filter)
        => _repository.GetProfitLossReportAsync(filter);

    public Task<AgingReportPagedResultDto<ReceivableAgingRowDto>> GetReceivableAgingReportAsync(ReportFilterDto filter)
        => _repository.GetReceivableAgingReportAsync(filter);

    public Task<AgingReportPagedResultDto<PayableAgingRowDto>> GetPayableAgingReportAsync(ReportFilterDto filter)
        => _repository.GetPayableAgingReportAsync(filter);
}
