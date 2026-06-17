using POSSystem.Application.Branch.DTOs;
using POSSystem.Application.Common.DTOs;

namespace POSSystem.Application.Branch.Interfaces;

public interface IBranchRepository
{
    Task<IReadOnlyList<BranchSummaryDto>> GetAllActiveSummariesAsync();
    Task<IReadOnlyList<BranchListItemDto>> GetByBusinessIdAsync(int businessId);
    Task<PagedResultDto<BranchListItemDto>> GetPagedAsync(
        int businessId,
        int page,
        int pageSize,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null);
    Task<BranchDetailDto?> GetDetailByIdAsync(int id, int businessId);
    Task<Domain.Branch?> GetTrackedByIdAsync(int id, int businessId);
    Task<bool> BusinessExistsAsync(int businessId);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<bool> CountryExistsAsync(int countryId);
    Task<bool> CityBelongsToCountryAsync(int cityId, int countryId);
    Task<string?> GetCityNameByIdAsync(int cityId);
    Task<IReadOnlyList<CountryListItemDto>> GetCountriesAsync();
    Task<IReadOnlyList<CityListItemDto>> GetCitiesByCountryIdAsync(int countryId);
    Task AddAsync(Domain.Branch branch);
    Task DeleteAsync(Domain.Branch branch);
    Task SaveChangesAsync();
}
