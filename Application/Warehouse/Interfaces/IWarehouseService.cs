using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Warehouse.DTOs;

namespace POSSystem.Application.Warehouse.Interfaces;

public interface IWarehouseService
{
    Task<PagedResultDto<WarehouseDto>> GetWarehousesPagedAsync(int businessId, int branchId, int page, int pageSize, string? search = null, bool? isActive = null);
    Task<List<WarehouseDto>> GetAllActiveAsync(int businessId, int branchId);
    Task<WarehouseDto?> GetWarehouseByIdAsync(int id, int businessId, int branchId);
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto);
    Task<WarehouseDto?> UpdateWarehouseAsync(int id, UpdateWarehouseDto dto);
    Task DeleteWarehouseAsync(int id, int businessId, int branchId);
}
