using POSSystem.Application.Reports.DTOs;

namespace POSSystem.Application.Reports.Interfaces;

public interface IReportRepository
{
    Task<ReportPagedResultDto<SalesReportRowDto>> GetSalesReportAsync(ReportFilterDto filter);
    Task<ReportPagedResultDto<PurchaseReportRowDto>> GetPurchaseReportAsync(ReportFilterDto filter);
    Task<ReportPagedResultDto<CustomerOutstandingRowDto>> GetCustomerOutstandingReportAsync(ReportFilterDto filter);
    Task<ReportPagedResultDto<SupplierPayableRowDto>> GetSupplierPayableReportAsync(ReportFilterDto filter);
    Task<ProfitLossReportPagedResultDto> GetProfitLossReportAsync(ReportFilterDto filter);
    Task<ProfitLossStatementDto> GetProfitLossStatementAsync(ReportFilterDto filter);
    Task<AgingReportPagedResultDto<ReceivableAgingRowDto>> GetReceivableAgingReportAsync(ReportFilterDto filter);
    Task<AgingReportPagedResultDto<PayableAgingRowDto>> GetPayableAgingReportAsync(ReportFilterDto filter);
    Task<ProductWiseSalesReportPagedResultDto> GetProductWiseSalesReportAsync(ReportFilterDto filter);
}
