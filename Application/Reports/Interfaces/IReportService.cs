using POSSystem.Application.Reports.DTOs;

namespace POSSystem.Application.Reports.Interfaces;

public interface IReportService
{
    Task<ReportPagedResultDto<SalesReportRowDto>> GetSalesReportAsync(ReportFilterDto filter);
    Task<ReportPagedResultDto<PurchaseReportRowDto>> GetPurchaseReportAsync(ReportFilterDto filter);
    Task<ReportPagedResultDto<CustomerOutstandingRowDto>> GetCustomerOutstandingReportAsync(ReportFilterDto filter);
    Task<ReportPagedResultDto<SupplierPayableRowDto>> GetSupplierPayableReportAsync(ReportFilterDto filter);
    Task<ReportPagedResultDto<ProfitLossRowDto>> GetProfitLossReportAsync(ReportFilterDto filter);
}
