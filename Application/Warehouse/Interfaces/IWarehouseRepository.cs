using POSSystem.Application.Common.DTOs;
using WarehouseEntity = POSSystem.Domain.Warehouse;

namespace POSSystem.Application.Warehouse.Interfaces;

public interface IWarehouseRepository
{
    Task<PagedResultDto<WarehouseEntity>> GetPagedAsync(int businessId, int branchId, int page, int pageSize, string? search = null, bool? isActive = null);
    Task<WarehouseEntity?> GetByIdAsync(int id, int businessId, int branchId);
    Task<WarehouseEntity?> GetByNameAsync(string name, int businessId, int branchId, int? excludeId = null);
    Task<List<WarehouseEntity>> GetAllActiveAsync(int businessId, int branchId);
    Task AddAsync(WarehouseEntity warehouse);
    Task SaveChangesAsync();
}
