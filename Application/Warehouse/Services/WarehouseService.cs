using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Warehouse.DTOs;
using POSSystem.Application.Warehouse.Interfaces;
using WarehouseEntity = POSSystem.Domain.Warehouse;

namespace POSSystem.Application.Warehouse.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _repository;

    public WarehouseService(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<WarehouseDto>> GetWarehousesPagedAsync(
        int businessId, int branchId, int page, int pageSize, string? search = null, bool? isActive = null)
    {
        var result = await _repository.GetPagedAsync(businessId, branchId, page, pageSize, search, isActive);
        return new PagedResultDto<WarehouseDto>
        {
            Data = result.Data.Select(MapDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<List<WarehouseDto>> GetAllActiveAsync(int businessId, int branchId)
    {
        var items = await _repository.GetAllActiveAsync(businessId, branchId);
        return items.Select(MapDto).ToList();
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(int id, int businessId, int branchId)
    {
        var entity = await _repository.GetByIdAsync(id, businessId, branchId);
        return entity == null ? null : MapDto(entity);
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Warehouse name is required.");

        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        var duplicate = await _repository.GetByNameAsync(dto.Name, dto.BusinessId, dto.BranchId);
        if (duplicate != null)
            throw new InvalidOperationException("A warehouse with this name already exists in the selected branch.");

        var entity = new WarehouseEntity
        {
            Name = dto.Name.Trim(),
            Code = dto.Code?.Trim() ?? string.Empty,
            Address = dto.Address?.Trim() ?? string.Empty,
            IsActive = dto.IsActive,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return MapDto(entity);
    }

    public async Task<WarehouseDto?> UpdateWarehouseAsync(int id, UpdateWarehouseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Warehouse name is required.");

        var entity = await _repository.GetByIdAsync(id, dto.BusinessId, dto.BranchId);
        if (entity == null)
            throw new InvalidOperationException("Warehouse not found.");

        var duplicate = await _repository.GetByNameAsync(dto.Name, dto.BusinessId, dto.BranchId, id);
        if (duplicate != null)
            throw new InvalidOperationException("A warehouse with this name already exists in the selected branch.");

        entity.Name = dto.Name.Trim();
        entity.Code = dto.Code?.Trim() ?? string.Empty;
        entity.Address = dto.Address?.Trim() ?? string.Empty;
        entity.IsActive = dto.IsActive;

        await _repository.SaveChangesAsync();
        return MapDto(entity);
    }

    public async Task DeleteWarehouseAsync(int id, int businessId, int branchId)
    {
        var entity = await _repository.GetByIdAsync(id, businessId, branchId);
        if (entity == null)
            throw new InvalidOperationException("Warehouse not found.");

        entity.IsDeleted = true;
        await _repository.SaveChangesAsync();
    }

    private static WarehouseDto MapDto(WarehouseEntity w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        Code = w.Code,
        Address = w.Address,
        IsActive = w.IsActive,
        BranchId = w.BranchId,
        BranchName = w.Branch?.Name ?? string.Empty,
        CreatedAt = w.CreatedAt,
        ModifiedAt = w.ModifiedAt
    };
}
