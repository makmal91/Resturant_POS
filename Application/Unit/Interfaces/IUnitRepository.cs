using MeasurementUnitEntity = POSSystem.Domain.MeasurementUnit;

namespace POSSystem.Application.Unit.Interfaces;

public interface IUnitRepository
{
    Task<List<MeasurementUnitEntity>> GetAllAsync(int businessId, int branchId, bool? status = null);
    Task<MeasurementUnitEntity?> GetByIdAsync(int id, int businessId, int branchId);
    Task<MeasurementUnitEntity?> GetByNameAsync(string name, int businessId, int branchId, int? excludeId = null);
    Task AddAsync(MeasurementUnitEntity unit);
    Task SaveChangesAsync();
}
