using POSSystem.Application.Accounting.DTOs;

namespace POSSystem.Application.Accounting.Interfaces;

public interface ITrialBalanceService
{
    Task<TrialBalanceReportDto> GetTrialBalanceAsync(TrialBalanceFilterDto filter);
}
