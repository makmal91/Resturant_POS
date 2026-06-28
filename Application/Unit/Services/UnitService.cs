using POSSystem.Application.Unit.DTOs;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Unit.Interfaces;
using MeasurementUnitEntity = POSSystem.Domain.MeasurementUnit;

namespace POSSystem.Application.Unit.Services;

public class UnitService : IUnitService
{
    private readonly IUnitRepository _repository;

    public UnitService(IUnitRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<UnitDto>> GetUnitsAsync(int businessId, int branchId, bool? status = null)
    {
        var units = await _repository.GetAllAsync(businessId, branchId, status);
        return units.Select(MapDto).ToList();
    }

    public async Task<PagedResultDto<UnitDto>> GetUnitsPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        bool? status = null,
        string? sortBy = null,
        string? sortDirection = null)
    {
        var result = await _repository.GetPagedAsync(
            businessId,
            branchId,
            page,
            pageSize,
            search,
            status,
            sortBy,
            sortDirection);

        return new PagedResultDto<UnitDto>
        {
            Data = result.Data.Select(MapDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<UnitDto?> GetUnitByIdAsync(int id, int businessId, int branchId)
    {
        var unit = await _repository.GetByIdAsync(id, businessId, branchId);
        return unit == null ? null : MapDto(unit);
    }

    public async Task<UnitDto> CreateUnitAsync(CreateUnitDto dto)
    {
        Validate(dto);
        var status = dto.IsActive ?? dto.Status;

        var duplicate = await _repository.GetByNameAsync(dto.Name, dto.BusinessId, dto.BranchId);
        if (duplicate != null)
            throw new InvalidOperationException("Unit name must be unique within the selected branch.");

        var unit = new MeasurementUnitEntity
        {
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            Name = dto.Name.Trim(),
            Code = dto.Code?.Trim() ?? string.Empty,
            DefaultConversionFactor = dto.DefaultConversionFactor > 0 ? dto.DefaultConversionFactor : 1m,
            Status = status
        };

        await _repository.AddAsync(unit);
        await _repository.SaveChangesAsync();

        return MapDto(unit);
    }

    public async Task<UnitDto> UpdateUnitAsync(int id, UpdateUnitDto dto)
    {
        Validate(dto);
        var unit = await _repository.GetByIdAsync(id, dto.BusinessId, dto.BranchId)
            ?? throw new InvalidOperationException("Unit not found.");

        var duplicate = await _repository.GetByNameAsync(dto.Name, dto.BusinessId, dto.BranchId, id);
        if (duplicate != null)
            throw new InvalidOperationException("Unit name must be unique within the selected branch.");

        unit.Name = dto.Name.Trim();
        unit.Code = dto.Code?.Trim() ?? string.Empty;
        unit.DefaultConversionFactor = dto.DefaultConversionFactor > 0 ? dto.DefaultConversionFactor : 1m;
        unit.Status = dto.IsActive ?? dto.Status;

        await _repository.SaveChangesAsync();
        return MapDto(unit);
    }

    public async Task DeleteUnitAsync(int id, int businessId, int branchId)
    {
        var unit = await _repository.GetByIdAsync(id, businessId, branchId)
            ?? throw new InvalidOperationException("Unit not found.");

        unit.IsDeleted = true;
        await _repository.SaveChangesAsync();
    }

    private static void Validate(CreateUnitDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Unit name is required.");

        if (dto.BranchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        if (dto.DefaultConversionFactor <= 0)
            throw new InvalidOperationException("Default conversion factor must be greater than zero.");
    }

    private static UnitDto MapDto(MeasurementUnitEntity unit)
    {
        return new UnitDto
        {
            Id = unit.Id,
            Name = unit.Name,
            Code = unit.Code,
            DefaultConversionFactor = unit.DefaultConversionFactor,
            Status = unit.Status,
            BranchId = unit.BranchId,
            BranchName = unit.Branch?.Name ?? string.Empty
        };
    }
}
