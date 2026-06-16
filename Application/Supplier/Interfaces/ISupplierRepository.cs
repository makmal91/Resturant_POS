using POSSystem.Application.Common.DTOs;
using SupplierEntity = POSSystem.Domain.Supplier;

namespace POSSystem.Application.Supplier.Interfaces;

public interface ISupplierRepository
{
    Task<PagedResultDto<SupplierEntity>> GetPagedAsync(int businessId, int branchId, int page, int pageSize, string? search = null, bool? isActive = null);
    Task<SupplierEntity?> GetByIdAsync(int id, int businessId, int branchId);
    Task<SupplierEntity?> GetByNameAsync(string name, int businessId, int branchId, int? excludeId = null);
    Task<bool> SupplierCodeExistsAsync(string supplierCode, int businessId, int branchId, int? excludeId = null);
    Task<List<SupplierEntity>> GetAllActiveAsync(int businessId, int branchId);
    Task AddAsync(SupplierEntity supplier);
    Task SaveChangesAsync();
}
