using POSSystem.Application.Unit.DTOs;

namespace POSSystem.Application.Unit.Interfaces;

public interface IUnitService
{
    Task<List<UnitDto>> GetUnitsAsync(int businessId, int branchId, bool? status = null);
    Task<UnitDto?> GetUnitByIdAsync(int id, int businessId, int branchId);
    Task<UnitDto> CreateUnitAsync(CreateUnitDto dto);
    Task<UnitDto> UpdateUnitAsync(int id, UpdateUnitDto dto);
    Task DeleteUnitAsync(int id, int businessId, int branchId);
}
