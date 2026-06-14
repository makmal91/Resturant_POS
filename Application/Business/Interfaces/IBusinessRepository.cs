using POSSystem.Application.Business.DTOs;
using POSSystem.Application.Common.DTOs;
using BusinessEntity = POSSystem.Domain.Business;

namespace POSSystem.Application.Business.Interfaces;

public interface IBusinessRepository
{
    Task<PagedResultDto<BusinessListItemDto>> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, string? sortDirection);
    Task<BusinessDetailDto?> GetDetailByIdAsync(int id);
    Task<BusinessLogoDto?> GetLogoByIdAsync(int id);
    Task<BusinessEntity?> GetTrackedByIdAsync(int id);
    Task<BusinessEntity?> GetTrackedWithBranchesAsync(int id);
    Task AddAsync(BusinessEntity business);
    Task SaveChangesAsync();
    void Remove(BusinessEntity business);
}
