using POSSystem.Application.Branch.DTOs;
using POSSystem.Application.Branch.Interfaces;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.License.Interfaces;
using BranchEntity = POSSystem.Domain.Branch;

namespace POSSystem.Application.Branch.Services;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _repository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly ILicenseEnforcementService _licenseEnforcement;

    public BranchService(
        IBranchRepository repository,
        ICodeGeneratorService codeGenerator,
        ILicenseEnforcementService licenseEnforcement)
    {
        _repository = repository;
        _codeGenerator = codeGenerator;
        _licenseEnforcement = licenseEnforcement;
    }
    public Task<IReadOnlyList<BranchListItemDto>> GetBranchesAsync(int businessId)
    {
        return _repository.GetByBusinessIdAsync(businessId);
    }

    public Task<BranchDetailDto?> GetBranchByIdAsync(int id, int businessId)
    {
        return _repository.GetDetailByIdAsync(id, businessId);
    }

    public async Task<BranchDetailDto> CreateBranchAsync(CreateBranchDto dto, int resolvedBusinessId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Branch name is required.");

        var businessId = ResolveBusinessId(dto, resolvedBusinessId);
        if (businessId <= 0)
            throw new InvalidOperationException("BusinessId is required.");

        if (dto.CountryId <= 0)
            throw new InvalidOperationException("Country is required.");

        if (dto.CityId <= 0)
            throw new InvalidOperationException("City is required.");

        if (!await _repository.BusinessExistsAsync(businessId))
            throw new InvalidOperationException("Invalid BusinessId. Business does not exist.");

        if (!await _repository.CountryExistsAsync(dto.CountryId))
            throw new InvalidOperationException("Invalid CountryId. Country does not exist.");

        if (!await _repository.CityBelongsToCountryAsync(dto.CityId, dto.CountryId))
            throw new InvalidOperationException("Invalid CityId. City does not belong to the selected country.");

        await _licenseEnforcement.EnsureCanCreateAsync(LicenseCreateOperation.Branch, businessId);

        var normalizedCode = (await _codeGenerator.ResolveAsync(CodeModuleNames.Branch, null, dto.Code))
            .ToUpperInvariant();

        if (await _repository.CodeExistsAsync(normalizedCode))
            throw new InvalidOperationException("Branch code already exists.");

        var branch = new BranchEntity
        {
            Name = dto.Name.Trim(),
            Code = normalizedCode,
            Address = dto.Address?.Trim() ?? string.Empty,
            Phone = dto.Phone?.Trim() ?? string.Empty,
            Email = dto.Email?.Trim() ?? string.Empty,
            OpeningTime = new TimeSpan(8, 0, 0),
            ClosingTime = new TimeSpan(23, 0, 0),
            IsActive = dto.IsActive,
            BusinessId = businessId,
            CountryId = dto.CountryId,
            CityId = dto.CityId
        };

        await _repository.AddAsync(branch);
        await _repository.SaveChangesAsync();

        return (await _repository.GetDetailByIdAsync(branch.Id, businessId))!;
    }

    public async Task<BranchDetailDto?> UpdateBranchAsync(int id, UpdateBranchDto dto, int resolvedBusinessId)
    {
        var businessId = dto.BusinessId > 0
            ? dto.BusinessId.Value
            : (dto.CompanyId > 0 ? dto.CompanyId.Value : resolvedBusinessId);

        var branch = await _repository.GetTrackedByIdAsync(id, resolvedBusinessId);
        if (branch == null)
            return null;

        if (dto.Name != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Branch name is required.");

            branch.Name = dto.Name.Trim();
        }

        if (dto.Code != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new InvalidOperationException("Branch code is required.");

            var normalizedCode = dto.Code.Trim().ToUpperInvariant();
            if (await _repository.CodeExistsAsync(normalizedCode, id))
                throw new InvalidOperationException("Branch code already exists.");

            branch.Code = normalizedCode;
        }

        if (dto.Address != null)
            branch.Address = dto.Address.Trim();

        if (dto.Phone != null)
            branch.Phone = dto.Phone.Trim();

        if (dto.Email != null)
            branch.Email = dto.Email.Trim();

        if (dto.IsActive.HasValue)
            branch.IsActive = dto.IsActive.Value;

        if (businessId > 0 && businessId != branch.BusinessId)
        {
            if (!await _repository.BusinessExistsAsync(businessId))
                throw new InvalidOperationException("Invalid BusinessId. Business does not exist.");

            branch.BusinessId = businessId;
        }

        var countryId = dto.CountryId ?? branch.CountryId;
        var cityId = dto.CityId ?? branch.CityId;

        if (dto.CountryId.HasValue || dto.CityId.HasValue)
        {
            if (countryId <= 0)
                throw new InvalidOperationException("Country is required.");

            if (cityId <= 0)
                throw new InvalidOperationException("City is required.");

            if (!await _repository.CountryExistsAsync(countryId))
                throw new InvalidOperationException("Invalid CountryId. Country does not exist.");

            if (!await _repository.CityBelongsToCountryAsync(cityId, countryId))
                throw new InvalidOperationException("Invalid CityId. City does not belong to the selected country.");

            branch.CountryId = countryId;
            branch.CityId = cityId;
        }

        await _repository.SaveChangesAsync();

        return await _repository.GetDetailByIdAsync(id, branch.BusinessId);
    }

    public async Task<bool> DeleteBranchAsync(int id, int businessId)
    {
        var branch = await _repository.GetTrackedByIdAsync(id, businessId);
        if (branch == null)
            return false;

        await _repository.DeleteAsync(branch);
        await _repository.SaveChangesAsync();
        return true;
    }

    public Task<IReadOnlyList<CountryListItemDto>> GetCountriesAsync()
    {
        return _repository.GetCountriesAsync();
    }

    public Task<IReadOnlyList<CityListItemDto>> GetCitiesByCountryIdAsync(int countryId)
    {
        return _repository.GetCitiesByCountryIdAsync(countryId);
    }

    private static int ResolveBusinessId(CreateBranchDto dto, int resolvedBusinessId)
    {
        if (dto.BusinessId > 0)
            return dto.BusinessId;

        if (dto.CompanyId > 0)
            return dto.CompanyId;

        return resolvedBusinessId;
    }
}
