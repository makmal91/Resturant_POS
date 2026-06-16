using POSSystem.Application.Business.DTOs;
using POSSystem.Application.Business.Interfaces;
using POSSystem.Application.Common.DTOs;
using BusinessEntity = POSSystem.Domain.Business;

namespace POSSystem.Application.Business.Services;

public class BusinessService : IBusinessService
{
    private readonly IBusinessRepository _repository;

    public BusinessService(IBusinessRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResultDto<BusinessListItemDto>> GetBusinessesAsync(int page, int pageSize, string? search = null, string? sortBy = null, string? sortDirection = null)
    {
        return _repository.GetPagedAsync(page, pageSize, search, sortBy, sortDirection);
    }

    public Task<BusinessDetailDto?> GetBusinessByIdAsync(int id)
    {
        return _repository.GetDetailByIdAsync(id);
    }

    public Task<BusinessLogoDto?> GetBusinessLogoAsync(int id)
    {
        return _repository.GetLogoByIdAsync(id);
    }

    public async Task<BusinessDetailDto> CreateBusinessAsync(
        CreateBusinessDto dto,
        byte[]? logo,
        string? logoFileName,
        string? logoContentType)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Business name is required.");

        var business = new BusinessEntity
        {
            Name = dto.Name.Trim(),
            LegalName = dto.LegalName?.Trim() ?? string.Empty,
            Phone = dto.Phone?.Trim() ?? string.Empty,
            Email = dto.Email?.Trim() ?? string.Empty,
            Address = dto.Address?.Trim() ?? string.Empty,
            TaxNumber = dto.TaxNumber?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.Trim().ToUpperInvariant(),
            TimeZone = string.IsNullOrWhiteSpace(dto.TimeZone) ? "UTC" : dto.TimeZone.Trim(),
            IsActive = dto.IsActive,
            Logo = logo,
            LogoFileName = logo == null ? null : logoFileName,
            LogoContentType = logo == null ? null : logoContentType
        };

        await _repository.AddAsync(business);
        await _repository.SaveChangesAsync();

        return (await _repository.GetDetailByIdAsync(business.Id))!;
    }

    public async Task<BusinessDetailDto?> UpdateBusinessAsync(
        int id,
        UpdateBusinessDto dto,
        byte[]? logo,
        string? logoFileName,
        string? logoContentType,
        bool replaceLogo)
    {
        var business = await _repository.GetTrackedByIdAsync(id);
        if (business == null)
            return null;

        if (dto.Name != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Business name is required.");

            business.Name = dto.Name.Trim();
        }

        if (dto.LegalName != null)
            business.LegalName = dto.LegalName.Trim();

        if (dto.Phone != null)
            business.Phone = dto.Phone.Trim();

        if (dto.Email != null)
            business.Email = dto.Email.Trim();

        if (dto.Address != null)
            business.Address = dto.Address.Trim();

        if (dto.TaxNumber != null)
            business.TaxNumber = dto.TaxNumber.Trim();

        if (dto.Currency != null)
            business.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.Trim().ToUpperInvariant();

        if (dto.TimeZone != null)
            business.TimeZone = string.IsNullOrWhiteSpace(dto.TimeZone) ? "UTC" : dto.TimeZone.Trim();

        if (dto.IsActive.HasValue)
            business.IsActive = dto.IsActive.Value;

        if (replaceLogo)
        {
            business.Logo = logo;
            business.LogoFileName = logo == null ? null : logoFileName;
            business.LogoContentType = logo == null ? null : logoContentType;
        }

        await _repository.SaveChangesAsync();

        return await _repository.GetDetailByIdAsync(id);
    }

    public async Task<bool> DeleteBusinessAsync(int id)
    {
        var business = await _repository.GetTrackedWithBranchesAsync(id);
        if (business == null)
            return false;

        if (business.Branches.Any(b => !b.IsDeleted))
            throw new InvalidOperationException($"Cannot delete business '{business.Name}' because it has {business.Branches.Count(b => !b.IsDeleted)} branch(es). Please delete all branches first.");

        _repository.Remove(business);
        await _repository.SaveChangesAsync();
        return true;
    }
}
