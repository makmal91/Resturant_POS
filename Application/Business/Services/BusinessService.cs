using POSSystem.Application.Business.DTOs;
using POSSystem.Application.Business.Interfaces;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Helpers;
using POSSystem.Application.License.Interfaces;
using BusinessEntity = POSSystem.Domain.Business;

namespace POSSystem.Application.Business.Services;

public class BusinessService : IBusinessService
{
    private readonly IBusinessRepository _repository;
    private readonly ILicenseEnforcementService _licenseEnforcement;

    public BusinessService(IBusinessRepository repository, ILicenseEnforcementService licenseEnforcement)
    {
        _repository = repository;
        _licenseEnforcement = licenseEnforcement;
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

        await _licenseEnforcement.EnsureCanCreateAsync(LicenseCreateOperation.Business);

        var (currencyId, currencyCode) = await ResolveCurrencyAsync(dto.CurrencyId, dto.Currency);

        var business = new BusinessEntity
        {
            Name = dto.Name.Trim(),
            LegalName = dto.LegalName?.Trim() ?? string.Empty,
            Phone = dto.Phone?.Trim() ?? string.Empty,
            Email = dto.Email?.Trim() ?? string.Empty,
            Address = dto.Address?.Trim() ?? string.Empty,
            TaxNumber = dto.TaxNumber?.Trim() ?? string.Empty,
            CurrencyId = currencyId,
            Currency = currencyCode,
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

        if (dto.CurrencyId.HasValue || dto.Currency != null)
        {
            var (currencyId, currencyCode) = await ResolveCurrencyAsync(dto.CurrencyId, dto.Currency);
            business.CurrencyId = currencyId;
            business.Currency = currencyCode;
        }

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

    private async Task<(int CurrencyId, string CurrencyCode)> ResolveCurrencyAsync(int? currencyId, string? currencyCode)
    {
        if (currencyId.HasValue && currencyId.Value > 0)
        {
            var byId = await _repository.GetCurrencyByIdAsync(currencyId.Value);
            if (byId == null)
                throw new InvalidOperationException("Invalid currency selected.");

            return (byId.Value.Id, byId.Value.Code);
        }

        if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            var byCode = await _repository.GetCurrencyByCodeAsync(currencyCode.Trim().ToUpperInvariant());
            if (byCode == null)
                throw new InvalidOperationException("Invalid currency code.");

            return (byCode.Value.Id, byCode.Value.Code);
        }

        return (1, CurrencyHelper.BaseCurrencyCode);
    }
}
