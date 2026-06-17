using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Unit.DTOs;

namespace POSSystem.Application.Unit.Interfaces;

public interface IUnitService
{
    Task<List<UnitDto>> GetUnitsAsync(int businessId, int branchId, bool? status = null);
    Task<PagedResultDto<UnitDto>> GetUnitsPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        bool? status = null,
        string? sortBy = null,
        string? sortDirection = null);
    Task<UnitDto?> GetUnitByIdAsync(int id, int businessId, int branchId);
    Task<UnitDto> CreateUnitAsync(CreateUnitDto dto);
    Task<UnitDto> UpdateUnitAsync(int id, UpdateUnitDto dto);
    Task DeleteUnitAsync(int id, int businessId, int branchId);
}
