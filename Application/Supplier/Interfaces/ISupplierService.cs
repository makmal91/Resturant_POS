using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Supplier.DTOs;

namespace POSSystem.Application.Supplier.Interfaces;

public interface ISupplierService
{
    Task<PagedResultDto<SupplierDto>> GetSuppliersPagedAsync(int businessId, int branchId, int page, int pageSize, string? search = null, bool? isActive = null);
    Task<List<SupplierDto>> GetAllActiveAsync(int businessId, int branchId);
    Task<SupplierDto?> GetSupplierByIdAsync(int id, int businessId, int branchId);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto);
    Task<SupplierDto?> UpdateSupplierAsync(int id, UpdateSupplierDto dto);
    Task DeleteSupplierAsync(int id, int businessId, int branchId);
}
