using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POSSystem.API.Extensions;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/masters")]
public class MastersController : ControllerBase
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "size",
        "color",
        "expense-category",
        "country",
        "city",
        "currency",
    };

    private static readonly HashSet<string> BranchScopedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "size",
        "color",
        "expense-category",
    };

    private static readonly HashSet<string> GlobalScopedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "country",
        "city",
    };

    private static bool IsMutableType(string type) =>
        BranchScopedTypes.Contains(type) || GlobalScopedTypes.Contains(type);

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly POSDbContext _db;
    private readonly IMemoryCache _cache;

    public MastersController(POSDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    [HttpGet("{type}")]
    public async Task<IActionResult> GetMasters(
        string type,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? countryId,
        [FromQuery] bool forManagement = false,
        [FromQuery] bool includeInactive = false)
    {
        if (!SupportedTypes.Contains(type))
            return BadRequest(new { message = $"Unsupported master type '{type}'." });

        var normalizedType = type.ToLowerInvariant();

        if (!forManagement)
        {
            var cacheKey = BuildCacheKey(normalizedType, businessId, branchId, countryId);
            if (_cache.TryGetValue(cacheKey, out List<MasterItemResponse>? cached) && cached != null)
                return Ok(cached);
        }

        var items = await QueryMastersAsync(normalizedType, businessId, branchId, countryId, forManagement, includeInactive);

        if (!forManagement)
        {
            var cacheKey = BuildCacheKey(normalizedType, businessId, branchId, countryId);
            _cache.Set(cacheKey, items.Select(i => new MasterItemResponse(i.Id, i.Name, i.HexCode)).ToList(), CacheDuration);
            return Ok(items.Select(i => new MasterItemResponse(i.Id, i.Name, i.HexCode)).ToList());
        }

        return Ok(items);
    }

    [HttpPost("{type}")]
    public async Task<IActionResult> CreateMaster(string type, [FromBody] SaveMasterRequest dto)
    {
        if (!IsMutableType(type))
            return BadRequest(new { message = $"Master type '{type}' is read-only." });

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Name is required." });

        var normalizedType = type.ToLowerInvariant();
        var name = dto.Name.Trim();

        try
        {
            if (GlobalScopedTypes.Contains(normalizedType))
            {
                var created = normalizedType switch
                {
                    "country" => await CreateCountryAsync(name, dto),
                    "city" => await CreateCityAsync(name, dto),
                    _ => null,
                };

                if (created == null)
                    return BadRequest(new { message = $"Unsupported master type '{type}'." });

                InvalidateGlobalCache(normalizedType, dto.CountryId);
                return Ok(created);
            }

            var biz = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
            var branch = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
            if (branch <= 0)
                return BadRequest(new { message = "branchId is required." });

            var branchCreated = normalizedType switch
            {
                "size" => await CreateSizeAsync(biz, branch, name, dto),
                "color" => await CreateColorAsync(biz, branch, name, dto),
                "expense-category" => await CreateExpenseCategoryAsync(biz, branch, name, dto),
                _ => null,
            };

            if (branchCreated == null)
                return BadRequest(new { message = $"Unsupported master type '{type}'." });

            InvalidateCache(normalizedType, biz, branch);
            return Ok(branchCreated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{type}/{id:int}")]
    public async Task<IActionResult> UpdateMaster(string type, int id, [FromBody] SaveMasterRequest dto)
    {
        if (!IsMutableType(type))
            return BadRequest(new { message = $"Master type '{type}' is read-only." });

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Name is required." });

        var normalizedType = type.ToLowerInvariant();
        var name = dto.Name.Trim();

        try
        {
            if (GlobalScopedTypes.Contains(normalizedType))
            {
                var updated = normalizedType switch
                {
                    "country" => await UpdateCountryAsync(id, name, dto),
                    "city" => await UpdateCityAsync(id, name, dto),
                    _ => null,
                };

                if (updated == null)
                    return NotFound(new { message = "Master record not found." });

                InvalidateGlobalCache(normalizedType, dto.CountryId);
                return Ok(updated);
            }

            var biz = this.ResolveBusinessId(dto.BusinessId > 0 ? dto.BusinessId : null);
            var branch = this.ResolveBranchId(dto.BranchId > 0 ? dto.BranchId : null);
            if (branch <= 0)
                return BadRequest(new { message = "branchId is required." });

            var branchUpdated = normalizedType switch
            {
                "size" => await UpdateSizeAsync(id, biz, branch, name, dto),
                "color" => await UpdateColorAsync(id, biz, branch, name, dto),
                "expense-category" => await UpdateExpenseCategoryAsync(id, biz, branch, name, dto),
                _ => null,
            };

            if (branchUpdated == null)
                return NotFound(new { message = "Master record not found." });

            InvalidateCache(normalizedType, biz, branch);
            return Ok(branchUpdated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{type}/{id:int}")]
    public async Task<IActionResult> DeleteMaster(
        string type,
        int id,
        [FromQuery] int? branchId,
        [FromQuery] int? businessId,
        [FromQuery] int? countryId)
    {
        if (!IsMutableType(type))
            return BadRequest(new { message = $"Master type '{type}' is read-only." });

        var normalizedType = type.ToLowerInvariant();

        try
        {
            if (GlobalScopedTypes.Contains(normalizedType))
            {
                var deleted = normalizedType switch
                {
                    "country" => await DeleteCountryAsync(id),
                    "city" => await DeleteCityAsync(id),
                    _ => false,
                };

                if (!deleted)
                    return NotFound(new { message = "Master record not found." });

                InvalidateGlobalCache(normalizedType, countryId);
                return NoContent();
            }

            var biz = this.ResolveBusinessId(businessId);
            var branch = this.ResolveBranchId(branchId);
            if (branch <= 0)
                return BadRequest(new { message = "branchId is required." });

            var branchDeleted = normalizedType switch
            {
                "size" => await DeleteSizeAsync(id, biz, branch),
                "color" => await DeleteColorAsync(id, biz, branch),
                "expense-category" => await DeleteExpenseCategoryAsync(id, biz, branch),
                _ => false,
            };

            if (!branchDeleted)
                return NotFound(new { message = "Master record not found." });

            InvalidateCache(normalizedType, biz, branch);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<List<MasterManageItemResponse>> QueryMastersAsync(
        string normalizedType,
        int? businessId,
        int? branchId,
        int? countryId,
        bool forManagement,
        bool includeInactive)
    {
        return normalizedType switch
        {
            "size" => await QuerySizesAsync(businessId, branchId, forManagement, includeInactive),
            "color" => await QueryColorsAsync(businessId, branchId, forManagement, includeInactive),
            "expense-category" => await QueryExpenseCategoriesAsync(businessId, branchId, forManagement, includeInactive),
            "country" => await QueryCountriesAsync(forManagement, includeInactive),
            "city" => await QueryCitiesAsync(countryId, forManagement, includeInactive),
            "currency" => await QueryCurrenciesAsync(),
            _ => [],
        };
    }

    private async Task<List<MasterManageItemResponse>> QuerySizesAsync(
        int? businessId, int? branchId, bool forManagement, bool includeInactive)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);
        if (branch <= 0)
            return [];

        var query = _db.Sizes.AsNoTracking()
            .Where(s => s.BusinessId == biz && s.BranchId == branch && !s.IsDeleted);

        if (!forManagement || !includeInactive)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Select(s => new MasterManageItemResponse(
                s.Id, s.Name, null, null, s.SortOrder, s.IsActive, null))
            .ToListAsync();
    }

    private async Task<List<MasterManageItemResponse>> QueryColorsAsync(
        int? businessId, int? branchId, bool forManagement, bool includeInactive)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);
        if (branch <= 0)
            return [];

        var query = _db.Colors.AsNoTracking()
            .Where(c => c.BusinessId == biz && c.BranchId == branch && !c.IsDeleted);

        if (!forManagement || !includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new MasterManageItemResponse(
                c.Id, c.Name, c.HexCode, null, 0, c.IsActive, null))
            .ToListAsync();
    }

    private async Task<List<MasterManageItemResponse>> QueryExpenseCategoriesAsync(
        int? businessId, int? branchId, bool forManagement, bool includeInactive)
    {
        var biz = this.ResolveBusinessId(businessId);
        var branch = this.ResolveBranchId(branchId);
        if (branch <= 0)
            return [];

        var query = _db.ExpenseCategories.AsNoTracking()
            .Where(c => c.BusinessId == biz && c.BranchId == branch && !c.IsDeleted);

        if (!forManagement || !includeInactive)
            query = query.Where(c => c.Status);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new MasterManageItemResponse(
                c.Id, c.Name, null, c.Description, 0, c.Status, null))
            .ToListAsync();
    }

    private async Task<List<MasterManageItemResponse>> QueryCountriesAsync(bool forManagement, bool includeInactive)
    {
        var query = _db.Countries.AsNoTracking().AsQueryable();
        if (!forManagement || !includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new MasterManageItemResponse(c.Id, c.Name, null, c.Code, 0, c.IsActive, null))
            .ToListAsync();
    }

    private async Task<List<MasterManageItemResponse>> QueryCitiesAsync(int? countryId, bool forManagement, bool includeInactive)
    {
        if (countryId is not > 0)
            return [];

        var query = _db.Cities.AsNoTracking().Where(c => c.CountryId == countryId);
        if (!forManagement || !includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new MasterManageItemResponse(c.Id, c.Name, null, null, 0, c.IsActive, c.CountryId))
            .ToListAsync();
    }

    private async Task<List<MasterManageItemResponse>> QueryCurrenciesAsync()
    {
        return await _db.Currencies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.IsBase ? 0 : 1)
            .ThenBy(c => c.Code)
            .Select(c => new MasterManageItemResponse(c.Id, c.Name, null, c.Code, 0, c.IsActive, null))
            .ToListAsync();
    }

    private async Task<int> GetNextCountryIdAsync() =>
        (await _db.Countries.MaxAsync(c => (int?)c.Id) ?? 0) + 1;

    private async Task<int> GetNextCityIdAsync() =>
        (await _db.Cities.MaxAsync(c => (int?)c.Id) ?? 0) + 1;

    private async Task<MasterManageItemResponse> CreateCountryAsync(string name, SaveMasterRequest dto)
    {
        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Country code is required.");

        if (await _db.Countries.AnyAsync(c => c.Code == code))
            throw new InvalidOperationException($"Country code '{code}' already exists.");

        if (await _db.Countries.AnyAsync(c => c.Name == name))
            throw new InvalidOperationException($"Country '{name}' already exists.");

        var entity = new Country
        {
            Id = await GetNextCountryIdAsync(),
            Name = name,
            Code = code,
            IsActive = dto.IsActive,
        };

        _db.Countries.Add(entity);
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, null, entity.Code, 0, entity.IsActive, null);
    }

    private async Task<MasterManageItemResponse> CreateCityAsync(string name, SaveMasterRequest dto)
    {
        if (dto.CountryId <= 0)
            throw new InvalidOperationException("CountryId is required.");

        if (!await _db.Countries.AnyAsync(c => c.Id == dto.CountryId && c.IsActive))
            throw new InvalidOperationException("Selected country was not found.");

        if (await _db.Cities.AnyAsync(c => c.CountryId == dto.CountryId && c.Name == name))
            throw new InvalidOperationException($"City '{name}' already exists for this country.");

        var entity = new City
        {
            Id = await GetNextCityIdAsync(),
            Name = name,
            CountryId = dto.CountryId,
            IsActive = dto.IsActive,
        };

        _db.Cities.Add(entity);
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, null, null, 0, entity.IsActive, entity.CountryId);
    }

    private async Task<MasterManageItemResponse?> UpdateCountryAsync(int id, string name, SaveMasterRequest dto)
    {
        var entity = await _db.Countries.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null)
            return null;

        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Country code is required.");

        if (await _db.Countries.AnyAsync(c => c.Id != id && c.Code == code))
            throw new InvalidOperationException($"Country code '{code}' already exists.");

        if (await _db.Countries.AnyAsync(c => c.Id != id && c.Name == name))
            throw new InvalidOperationException($"Country '{name}' already exists.");

        entity.Name = name;
        entity.Code = code;
        entity.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, null, entity.Code, 0, entity.IsActive, null);
    }

    private async Task<MasterManageItemResponse?> UpdateCityAsync(int id, string name, SaveMasterRequest dto)
    {
        var entity = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null)
            return null;

        var countryId = dto.CountryId > 0 ? dto.CountryId : entity.CountryId;
        if (!await _db.Countries.AnyAsync(c => c.Id == countryId && c.IsActive))
            throw new InvalidOperationException("Selected country was not found.");

        if (await _db.Cities.AnyAsync(c =>
                c.Id != id && c.CountryId == countryId && c.Name == name))
            throw new InvalidOperationException($"City '{name}' already exists for this country.");

        entity.Name = name;
        entity.CountryId = countryId;
        entity.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, null, null, 0, entity.IsActive, entity.CountryId);
    }

    private async Task<bool> DeleteCountryAsync(int id)
    {
        var entity = await _db.Countries.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null)
            return false;

        var inUse = await _db.Branches.AnyAsync(b => b.CountryId == id && !b.IsDeleted)
            || await _db.Customers.AnyAsync(c => c.CountryId == id && !c.IsDeleted);
        if (inUse)
            throw new InvalidOperationException("Cannot delete country because it is referenced by branches or customers.");

        var hasCities = await _db.Cities.AnyAsync(c => c.CountryId == id && c.IsActive);
        if (hasCities)
            throw new InvalidOperationException("Cannot delete country while it has active cities. Deactivate cities first.");

        entity.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<bool> DeleteCityAsync(int id)
    {
        var entity = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null)
            return false;

        var inUse = await _db.Branches.AnyAsync(b => b.CityId == id && !b.IsDeleted)
            || await _db.Customers.AnyAsync(c => c.CityId == id && !c.IsDeleted);
        if (inUse)
            throw new InvalidOperationException("Cannot delete city because it is referenced by branches or customers.");

        entity.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<MasterManageItemResponse> CreateSizeAsync(int biz, int branch, string name, SaveMasterRequest dto)
    {
        if (await _db.Sizes.AnyAsync(s =>
                s.BusinessId == biz && s.BranchId == branch && s.Name == name && !s.IsDeleted))
            throw new InvalidOperationException($"Size '{name}' already exists.");

        var entity = new ProductSize
        {
            BusinessId = biz,
            BranchId = branch,
            Name = name,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive,
        };

        _db.Sizes.Add(entity);
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, null, null, entity.SortOrder, entity.IsActive, null);
    }

    private async Task<MasterManageItemResponse> CreateColorAsync(int biz, int branch, string name, SaveMasterRequest dto)
    {
        if (await _db.Colors.AnyAsync(c =>
                c.BusinessId == biz && c.BranchId == branch && c.Name == name && !c.IsDeleted))
            throw new InvalidOperationException($"Color '{name}' already exists.");

        var entity = new ProductColor
        {
            BusinessId = biz,
            BranchId = branch,
            Name = name,
            HexCode = string.IsNullOrWhiteSpace(dto.HexCode) ? null : dto.HexCode.Trim(),
            IsActive = dto.IsActive,
        };

        _db.Colors.Add(entity);
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, entity.HexCode, null, 0, entity.IsActive, null);
    }

    private async Task<MasterManageItemResponse> CreateExpenseCategoryAsync(int biz, int branch, string name, SaveMasterRequest dto)
    {
        if (await _db.ExpenseCategories.AnyAsync(c =>
                c.BusinessId == biz && c.BranchId == branch && c.Name == name && !c.IsDeleted))
            throw new InvalidOperationException($"Expense category '{name}' already exists.");

        var entity = new ExpenseCategory
        {
            BusinessId = biz,
            BranchId = branch,
            Name = name,
            Description = dto.Description?.Trim(),
            Status = dto.IsActive,
        };

        _db.ExpenseCategories.Add(entity);
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, null, entity.Description, 0, entity.Status, null);
    }

    private async Task<MasterManageItemResponse?> UpdateSizeAsync(int id, int biz, int branch, string name, SaveMasterRequest dto)
    {
        var entity = await _db.Sizes.FirstOrDefaultAsync(s =>
            s.Id == id && s.BusinessId == biz && s.BranchId == branch && !s.IsDeleted);
        if (entity == null)
            return null;

        if (await _db.Sizes.AnyAsync(s =>
                s.Id != id && s.BusinessId == biz && s.BranchId == branch && s.Name == name && !s.IsDeleted))
            throw new InvalidOperationException($"Size '{name}' already exists.");

        entity.Name = name;
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;
        entity.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, null, null, entity.SortOrder, entity.IsActive, null);
    }

    private async Task<MasterManageItemResponse?> UpdateColorAsync(int id, int biz, int branch, string name, SaveMasterRequest dto)
    {
        var entity = await _db.Colors.FirstOrDefaultAsync(c =>
            c.Id == id && c.BusinessId == biz && c.BranchId == branch && !c.IsDeleted);
        if (entity == null)
            return null;

        if (await _db.Colors.AnyAsync(c =>
                c.Id != id && c.BusinessId == biz && c.BranchId == branch && c.Name == name && !c.IsDeleted))
            throw new InvalidOperationException($"Color '{name}' already exists.");

        entity.Name = name;
        entity.HexCode = string.IsNullOrWhiteSpace(dto.HexCode) ? null : dto.HexCode.Trim();
        entity.IsActive = dto.IsActive;
        entity.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, entity.HexCode, null, 0, entity.IsActive, null);
    }

    private async Task<MasterManageItemResponse?> UpdateExpenseCategoryAsync(int id, int biz, int branch, string name, SaveMasterRequest dto)
    {
        var entity = await _db.ExpenseCategories.FirstOrDefaultAsync(c =>
            c.Id == id && c.BusinessId == biz && c.BranchId == branch && !c.IsDeleted);
        if (entity == null)
            return null;

        if (await _db.ExpenseCategories.AnyAsync(c =>
                c.Id != id && c.BusinessId == biz && c.BranchId == branch && c.Name == name && !c.IsDeleted))
            throw new InvalidOperationException($"Expense category '{name}' already exists.");

        entity.Name = name;
        entity.Description = dto.Description?.Trim();
        entity.Status = dto.IsActive;
        entity.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new MasterManageItemResponse(entity.Id, entity.Name, null, entity.Description, 0, entity.Status, null);
    }

    private async Task<bool> DeleteSizeAsync(int id, int biz, int branch)
    {
        var entity = await _db.Sizes.FirstOrDefaultAsync(s =>
            s.Id == id && s.BusinessId == biz && s.BranchId == branch && !s.IsDeleted);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<bool> DeleteColorAsync(int id, int biz, int branch)
    {
        var entity = await _db.Colors.FirstOrDefaultAsync(c =>
            c.Id == id && c.BusinessId == biz && c.BranchId == branch && !c.IsDeleted);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<bool> DeleteExpenseCategoryAsync(int id, int biz, int branch)
    {
        var entity = await _db.ExpenseCategories.FirstOrDefaultAsync(c =>
            c.Id == id && c.BusinessId == biz && c.BranchId == branch && !c.IsDeleted);
        if (entity == null)
            return false;

        var inUse = await _db.Expenses.AnyAsync(e =>
            e.ExpenseCategoryId == id && e.BusinessId == biz && e.BranchId == branch && !e.IsDeleted);
        if (inUse)
            throw new InvalidOperationException("Cannot delete category because it is referenced by expenses.");

        entity.IsDeleted = true;
        entity.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private void InvalidateCache(string type, int businessId, int branchId)
    {
        _cache.Remove(BuildCacheKey(type, businessId, branchId, null));
        _cache.Remove(BuildCacheKey(type, businessId, branchId, 0));
    }

    private void InvalidateGlobalCache(string type, int? countryId)
    {
        _cache.Remove(BuildCacheKey(type, null, null, null));
        _cache.Remove(BuildCacheKey(type, null, null, 0));
        if (countryId is > 0)
            _cache.Remove(BuildCacheKey("city", null, null, countryId));
        _cache.Remove(BuildCacheKey("country", null, null, null));
    }

    private static string BuildCacheKey(string type, int? businessId, int? branchId, int? countryId) =>
        $"masters:{type}:{businessId ?? 0}:{branchId ?? 0}:{countryId ?? 0}";
}

public record MasterItemResponse(int Id, string Name, string? HexCode);

public record MasterManageItemResponse(
    int Id,
    string Name,
    string? HexCode,
    string? Description,
    int SortOrder,
    bool IsActive,
    int? CountryId);

public class SaveMasterRequest
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? HexCode { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
