using POSSystem.Application.Branch.DTOs;

namespace POSSystem.Application.Branch.Interfaces;

public interface IBranchService
{
    Task<IReadOnlyList<BranchListItemDto>> GetBranchesAsync(int businessId);
    Task<BranchDetailDto?> GetBranchByIdAsync(int id, int businessId);
    Task<BranchDetailDto> CreateBranchAsync(CreateBranchDto dto, int resolvedBusinessId);
    Task<BranchDetailDto?> UpdateBranchAsync(int id, UpdateBranchDto dto, int resolvedBusinessId);
    Task<bool> DeleteBranchAsync(int id, int businessId);
    Task<IReadOnlyList<CountryListItemDto>> GetCountriesAsync();
    Task<IReadOnlyList<CityListItemDto>> GetCitiesByCountryIdAsync(int countryId);
}
