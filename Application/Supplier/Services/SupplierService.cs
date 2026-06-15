using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Supplier.DTOs;
using POSSystem.Application.Supplier.Interfaces;
using SupplierEntity = POSSystem.Domain.Supplier;

namespace POSSystem.Application.Supplier.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repository;

    public SupplierService(ISupplierRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<SupplierDto>> GetSuppliersPagedAsync(
        int businessId, int branchId, int page, int pageSize, string? search = null, bool? isActive = null)
    {
        var result = await _repository.GetPagedAsync(businessId, branchId, page, pageSize, search, isActive);
        return new PagedResultDto<SupplierDto>
        {
            Data = result.Data.Select(MapDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<List<SupplierDto>> GetAllActiveAsync(int businessId, int branchId)
    {
        var items = await _repository.GetAllActiveAsync(businessId, branchId);
        return items.Select(MapDto).ToList();
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id, int businessId, int branchId)
    {
        var entity = await _repository.GetByIdAsync(id, businessId, branchId);
        return entity == null ? null : MapDto(entity);
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Supplier name is required.");

        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        var duplicate = await _repository.GetByNameAsync(dto.Name, dto.BusinessId, dto.BranchId);
        if (duplicate != null)
            throw new InvalidOperationException("A supplier with this name already exists in the selected branch.");

        var entity = new SupplierEntity
        {
            Name = dto.Name.Trim(),
            ContactPerson = dto.ContactPerson?.Trim() ?? string.Empty,
            Phone = dto.Phone?.Trim() ?? string.Empty,
            Email = dto.Email?.Trim() ?? string.Empty,
            Address = dto.Address?.Trim() ?? string.Empty,
            TaxNumber = dto.TaxNumber?.Trim() ?? string.Empty,
            IsActive = dto.IsActive,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return MapDto(entity);
    }

    public async Task<SupplierDto?> UpdateSupplierAsync(int id, UpdateSupplierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Supplier name is required.");

        var entity = await _repository.GetByIdAsync(id, dto.BusinessId, dto.BranchId);
        if (entity == null)
            throw new InvalidOperationException("Supplier not found.");

        var duplicate = await _repository.GetByNameAsync(dto.Name, dto.BusinessId, dto.BranchId, id);
        if (duplicate != null)
            throw new InvalidOperationException("A supplier with this name already exists in the selected branch.");

        entity.Name = dto.Name.Trim();
        entity.ContactPerson = dto.ContactPerson?.Trim() ?? string.Empty;
        entity.Phone = dto.Phone?.Trim() ?? string.Empty;
        entity.Email = dto.Email?.Trim() ?? string.Empty;
        entity.Address = dto.Address?.Trim() ?? string.Empty;
        entity.TaxNumber = dto.TaxNumber?.Trim() ?? string.Empty;
        entity.IsActive = dto.IsActive;
        entity.UpdatedDate = DateTime.UtcNow;

        await _repository.SaveChangesAsync();
        return MapDto(entity);
    }

    public async Task DeleteSupplierAsync(int id, int businessId, int branchId)
    {
        var entity = await _repository.GetByIdAsync(id, businessId, branchId);
        if (entity == null)
            throw new InvalidOperationException("Supplier not found.");

        entity.IsDeleted = true;
        entity.UpdatedDate = DateTime.UtcNow;
        await _repository.SaveChangesAsync();
    }

    private static SupplierDto MapDto(SupplierEntity s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        ContactPerson = s.ContactPerson,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address,
        TaxNumber = s.TaxNumber,
        IsActive = s.IsActive,
        BranchId = s.BranchId,
        BranchName = s.Branch?.Name ?? string.Empty,
        CreatedDate = s.CreatedDate,
        UpdatedDate = s.UpdatedDate
    };
}
