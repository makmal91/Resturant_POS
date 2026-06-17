using MeasurementUnitEntity = POSSystem.Domain.MeasurementUnit;
using POSSystem.Application.Common.DTOs;

namespace POSSystem.Application.Unit.Interfaces;

public interface IUnitRepository
{
    Task<List<MeasurementUnitEntity>> GetAllAsync(int businessId, int branchId, bool? status = null);
    Task<PagedResultDto<MeasurementUnitEntity>> GetPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        bool? status = null,
        string? sortBy = null,
        string? sortDirection = null);
    Task<MeasurementUnitEntity?> GetByIdAsync(int id, int businessId, int branchId);
    Task<MeasurementUnitEntity?> GetByNameAsync(string name, int businessId, int branchId, int? excludeId = null);
    Task AddAsync(MeasurementUnitEntity unit);
    Task SaveChangesAsync();
}
