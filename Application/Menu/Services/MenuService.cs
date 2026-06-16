using Microsoft.Extensions.Caching.Memory;
using POSSystem.Application.Common.Constants;
using POSSystem.Application.Common.DTOs;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Application.Menu.DTOs;
using POSSystem.Application.Menu.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.Menu.Services;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ICodeGeneratorService _codeGenerator;

    private static readonly TimeSpan PosCacheDuration = TimeSpan.FromMinutes(2);

    private static (bool isSaleable, bool isInventoryItem, bool isRecipeItem, bool isPurchasable) ResolveFlagsByProductType(ProductType productType)
    {
        return productType switch
        {
            ProductType.RawMaterial => (false, true, true, true),
            ProductType.FinishedGood => (true, false, false, false),
            ProductType.SemiFinished => (false, true, true, false),
            ProductType.Service => (true, false, false, false),
            _ => (true, false, false, false)
        };
    }

    private static void ValidateOptionalFlags(ProductType productType, bool? isSaleable, bool? isInventoryItem, bool? isRecipeItem, bool? isPurchasable, (bool isSaleable, bool isInventoryItem, bool isRecipeItem, bool isPurchasable) expected)
    {
        if (isSaleable.HasValue && isSaleable.Value != expected.isSaleable)
            throw new InvalidOperationException($"Invalid IsSaleable for product type {productType}.");

        if (isInventoryItem.HasValue && isInventoryItem.Value != expected.isInventoryItem)
            throw new InvalidOperationException($"Invalid IsInventoryItem for product type {productType}.");

        if (isRecipeItem.HasValue && isRecipeItem.Value != expected.isRecipeItem)
            throw new InvalidOperationException($"Invalid IsRecipeItem for product type {productType}.");

        if (isPurchasable.HasValue && isPurchasable.Value != expected.isPurchasable)
            throw new InvalidOperationException($"Invalid IsPurchasable for product type {productType}.");
    }

    private static void ValidateCategoryProductCompatibility(CategoryType categoryType, ProductType productType)
    {
        if (categoryType == CategoryType.Sale && productType != ProductType.FinishedGood)
            throw new InvalidOperationException("Sale categories only allow FinishedGood items.");

        if (categoryType == CategoryType.Inventory &&
            productType != ProductType.RawMaterial &&
            productType != ProductType.SemiFinished)
            throw new InvalidOperationException("Inventory categories only allow RawMaterial or SemiFinished items.");
    }

    private static MenuCategoryDto MapCategoryDto(MenuCategory category)
    {
        return new MenuCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Code = category.Code,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            ImageUrl = category.ImageUrl,
            HasImage = category.Image != null && category.Image.Length > 0,
            Icon = category.Icon,
            Color = category.Color,
            Status = category.Status,
            CategoryType = category.CategoryType,
            BusinessId = category.BusinessId,
            BranchId = category.BranchId,
            BranchName = category.Branch?.Name ?? string.Empty,
            SubCategories = category.SubCategories.Where(sc => sc.Status).Select(sc => MapSubCategoryDto(sc)).ToList(),
            Items = category.MenuItems.Select(MapMenuItemDto).ToList()
        };
    }

    private static SubCategoryDto MapSubCategoryDto(SubCategory subCategory, bool includeImage = false)
    {
        var hasImage = subCategory.ImageData != null && subCategory.ImageData.Length > 0;
        string? imageDataUri = null;

        if (includeImage && hasImage)
        {
            var contentType = string.IsNullOrWhiteSpace(subCategory.ImageContentType)
                ? "application/octet-stream"
                : subCategory.ImageContentType;
            imageDataUri = $"data:{contentType};base64,{Convert.ToBase64String(subCategory.ImageData!)}";
        }

        return new SubCategoryDto
        {
            Id = subCategory.Id,
            Name = subCategory.Name,
            Code = subCategory.Code,
            Description = subCategory.Description,
            DisplayOrder = subCategory.DisplayOrder,
            Status = subCategory.Status,
            Icon = subCategory.Icon,
            HasImage = hasImage,
            Image = imageDataUri,
            CategoryId = subCategory.CategoryId,
            CategoryName = subCategory.Category?.Name ?? string.Empty,
            BusinessId = subCategory.BusinessId,
            BranchId = subCategory.BranchId,
            BranchName = subCategory.Branch?.Name ?? string.Empty,
            CreatedAt = subCategory.CreatedAt,
            ModifiedAt = subCategory.ModifiedAt
        };
    }

    private static void ApplySubCategoryImage(
        SubCategory subCategory,
        byte[]? imageBytes,
        string? contentType,
        bool replaceImage,
        bool removeImage)
    {
        if (!replaceImage)
            return;

        if (removeImage || imageBytes == null || imageBytes.Length == 0)
        {
            subCategory.ImageData = null;
            subCategory.ImageContentType = null;
            return;
        }

        subCategory.ImageData = imageBytes;
        subCategory.ImageContentType = contentType;
    }

    private void InvalidateSubCategoryCaches(int businessId, int branchId)
    {
        _cache.Remove(GetPosMenuCacheKey(businessId, branchId));
    }

    private static MenuItemDto MapMenuItemDto(MenuItem item)
    {
        return new MenuItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            Tax = item.TaxPercentage,
            PreparationTime = item.PreparationTime,
            MenuCategoryId = item.MenuCategoryId,
            BusinessId = item.BusinessId,
            BranchId = item.BranchId,
            ProductType = item.ProductType,
            IsSaleable = item.IsSaleable,
            IsInventoryItem = item.IsInventoryItem,
            IsRecipeItem = item.IsRecipeItem,
            IsPurchasable = item.IsPurchasable,
            Variants = item.Variants.Select(v => new MenuItemVariantDto
            {
                Id = v.Id,
                Name = v.Name,
                Price = v.Price
            }).ToList(),
            Addons = item.Addons.Select(a => new MenuItemAddonDto
            {
                Id = a.Id,
                Name = a.Name,
                Price = a.Price
            }).ToList()
        };
    }

    public MenuService(IMenuRepository repository, IMemoryCache cache, ICodeGeneratorService codeGenerator)
    {
        _repository = repository;
        _cache = cache;
        _codeGenerator = codeGenerator;
    }

    private static string GetPosMenuCacheKey(int businessId, int branchId) => $"pos-menu-{businessId}-{branchId}";
    private static string GetCategoryListVersionKey(int businessId, int branchId) => $"categories-version-{businessId}-{branchId}";
    private static string GetCategoryListCacheKey(int businessId, int branchId, int version, int page, int pageSize, CategoryType? categoryType) =>
        $"categories-{businessId}-{branchId}-{version}-{page}-{pageSize}-{categoryType?.ToString() ?? "all"}";

    private int GetCategoryListVersion(int businessId, int branchId)
    {
        return _cache.GetOrCreate(GetCategoryListVersionKey(businessId, branchId), entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return 0;
        });
    }

    private void InvalidateCategoryListCache(int businessId, int branchId)
    {
        _cache.Set(GetCategoryListVersionKey(businessId, branchId), GetCategoryListVersion(businessId, branchId) + 1);
        _cache.Set(GetCategoryListVersionKey(businessId, 0), GetCategoryListVersion(businessId, 0) + 1);
    }

    private async Task EnsureBranchExistsAsync(int businessId, int branchId)
    {
        if (businessId <= 0)
            throw new InvalidOperationException("BusinessId is required.");

        if (branchId <= 0)
            throw new InvalidOperationException("BranchId is required.");

        var branchExists = await _repository.BranchExistsAsync(businessId, branchId);
        if (!branchExists)
            throw new InvalidOperationException("Selected branch does not exist.");
    }

    private async Task ValidateCategoryInputAsync(string categoryName, string? categoryCode, int businessId, int branchId, int? excludeCategoryId = null)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new InvalidOperationException("Category name is required.");

        await EnsureBranchExistsAsync(businessId, branchId);

        var duplicateCategory = await _repository.GetCategoryByNameAsync(categoryName, businessId, branchId, excludeCategoryId);
        if (duplicateCategory != null && !duplicateCategory.IsDeleted)
            throw new InvalidOperationException("Category name must be unique per branch.");

        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            if (await _repository.CategoryCodeExistsAsync(categoryCode, businessId, branchId, excludeCategoryId))
                throw new InvalidOperationException("Category code must be unique per branch.");
        }
    }

    private async Task<string> ResolveCategoryCodeAsync(string? requestedCode, int branchId)
    {
        if (string.IsNullOrWhiteSpace(requestedCode))
            return await _codeGenerator.GenerateAsync(CodeModuleNames.Category, branchId);

        return requestedCode.Trim();
    }

    private async Task<string> ResolveSubCategoryCodeAsync(string? requestedCode, int branchId)
    {
        if (string.IsNullOrWhiteSpace(requestedCode))
            return await _codeGenerator.GenerateAsync(CodeModuleNames.SubCategory, branchId);

        return requestedCode.Trim();
    }

    private static void ApplyCreateDtoToCategory(MenuCategory category, CreateMenuCategoryDto dto, string resolvedCode)
    {
        category.Name = dto.Name.Trim();
        category.Code = resolvedCode;
        category.Description = dto.Description;
        category.DisplayOrder = dto.DisplayOrder;
        category.ImageUrl = dto.ImageUrl;
        category.Icon = dto.Icon;
        category.Color = dto.Color;
        category.Status = dto.Status;
        category.CategoryType = dto.CategoryType;
        category.BusinessId = dto.BusinessId;
        category.BranchId = dto.BranchId;
    }

    private static void ApplyCategoryImage(
        MenuCategory category,
        byte[]? imageBytes,
        string? fileName,
        string? contentType,
        bool replaceImage,
        bool removeImage)
    {
        if (!replaceImage)
            return;

        if (removeImage || imageBytes == null || imageBytes.Length == 0)
        {
            category.Image = null;
            category.ImageContentType = null;
            category.ImageFileName = null;
            return;
        }

        category.Image = imageBytes;
        category.ImageContentType = contentType;
        category.ImageFileName = fileName;
    }

    private static bool IsDuplicateKeyException(Exception exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("2601", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<MenuCategory> ValidateSubCategoryContextAsync(int categoryId, int businessId, int branchId)
    {
        var category = await _repository.GetCategoryAsync(categoryId, businessId, branchId);
        if (category == null)
            throw new InvalidOperationException("Category not found.");

        if (category.BranchId != branchId)
            throw new InvalidOperationException("Category branch mismatch.");

        return category;
    }

    public async Task<ICollection<MenuCategoryDto>> GetCategoriesAsync(int businessId, int branchId, CategoryType? categoryType = null)
    {
        await EnsureBranchExistsAsync(businessId, branchId);

        var categories = await _repository.GetCategoriesByBranchAsync(businessId, branchId, categoryType);
        return categories.Select(MapCategoryDto).ToList();
    }

    public async Task<PagedResultDto<MenuCategoryDto>> GetCategoriesPagedAsync(int businessId, int branchId, int page, int pageSize, CategoryType? categoryType = null)
    {
        if (businessId <= 0)
            throw new InvalidOperationException("BusinessId is required.");

        if (branchId < 0)
            throw new InvalidOperationException("BranchId is required.");

        if (branchId > 0)
        {
            await EnsureBranchExistsAsync(businessId, branchId);
        }

        var cacheKey = GetCategoryListCacheKey(
            businessId,
            branchId,
            GetCategoryListVersion(businessId, branchId),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            categoryType);
        if (_cache.TryGetValue(cacheKey, out PagedResultDto<MenuCategoryDto>? cached) && cached != null)
        {
            return cached;
        }

        var result = await _repository.GetCategoriesPagedAsync(businessId, branchId, page, pageSize, categoryType);
        var dto = new PagedResultDto<MenuCategoryDto>
        {
            Data = result.Data.Select(MapCategoryDto).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };

        _cache.Set(cacheKey, dto, PosCacheDuration);
        return dto;
    }

    public async Task<MenuCategoryDto?> GetCategoryByIdAsync(int id, int businessId, int branchId)
    {
        await EnsureBranchExistsAsync(businessId, branchId);

        var category = await _repository.GetCategoryAsync(id, businessId, branchId, includeItems: true);
        if (category == null || category.BranchId != branchId)
            return null;

        return MapCategoryDto(category);
    }

    public async Task<CategoryImageDto?> GetCategoryImageAsync(int id, int businessId, int branchId)
    {
        await EnsureBranchExistsAsync(businessId, branchId);

        var category = await _repository.GetCategoryAsync(id, businessId, branchId);
        if (category == null || category.BranchId != branchId || category.Image == null || category.Image.Length == 0)
            return null;

        return new CategoryImageDto
        {
            Image = category.Image,
            ImageContentType = category.ImageContentType ?? string.Empty,
            ImageFileName = category.ImageFileName ?? string.Empty
        };
    }

    public async Task<MenuCategory> AddCategoryAsync(
        CreateMenuCategoryDto dto,
        byte[]? imageBytes = null,
        string? imageFileName = null,
        string? imageContentType = null)
    {
        await ValidateCategoryInputAsync(dto.Name, dto.Code, dto.BusinessId, dto.BranchId);

        var resolvedCode = await ResolveCategoryCodeAsync(dto.Code, dto.BranchId);
        var hasUploadedImage = imageBytes != null && imageBytes.Length > 0;

        var archivedCategory = await _repository.GetCategoryByNameAsync(dto.Name, dto.BusinessId, dto.BranchId);
        if (archivedCategory != null && archivedCategory.IsDeleted)
        {
            ApplyCreateDtoToCategory(archivedCategory, dto, resolvedCode);
            ApplyCategoryImage(archivedCategory, imageBytes, imageFileName, imageContentType, hasUploadedImage, false);
            archivedCategory.IsDeleted = false;

            try
            {
                await _repository.SaveChangesAsync();
            }
            catch (Exception ex) when (IsDuplicateKeyException(ex))
            {
                throw new InvalidOperationException("Category name must be unique per branch.");
            }

            _cache.Remove(GetPosMenuCacheKey(dto.BusinessId, dto.BranchId));
            InvalidateCategoryListCache(dto.BusinessId, dto.BranchId);
            return archivedCategory;
        }

        var category = new MenuCategory();
        ApplyCreateDtoToCategory(category, dto, resolvedCode);
        ApplyCategoryImage(category, imageBytes, imageFileName, imageContentType, hasUploadedImage, false);

        try
        {
            await _repository.AddCategoryAsync(category);
            await _repository.SaveChangesAsync();
        }
        catch (Exception ex) when (IsDuplicateKeyException(ex))
        {
            throw new InvalidOperationException("Category name must be unique per branch.");
        }

        _cache.Remove(GetPosMenuCacheKey(dto.BusinessId, dto.BranchId));
        InvalidateCategoryListCache(dto.BusinessId, dto.BranchId);

        return category;
    }

    public async Task<MenuCategory> UpdateCategoryAsync(
        int id,
        UpdateMenuCategoryDto dto,
        byte[]? imageBytes = null,
        string? imageFileName = null,
        string? imageContentType = null,
        bool replaceImage = false)
    {
        var category = await _repository.GetCategoryAsync(id, dto.BusinessId, dto.BranchId, includeItems: true);
        if (category == null)
            throw new InvalidOperationException("Category not found.");

        await ValidateCategoryInputAsync(dto.Name, dto.Code, dto.BusinessId, dto.BranchId, excludeCategoryId: id);

        if (category.BranchId != dto.BranchId)
            throw new InvalidOperationException("Category branch mismatch.");

        if (dto.CategoryType != category.CategoryType)
        {
            foreach (var item in category.MenuItems)
            {
                ValidateCategoryProductCompatibility(dto.CategoryType, item.ProductType);
            }
        }

        category.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Code))
            category.Code = dto.Code.Trim();
        category.Description = dto.Description;
        category.DisplayOrder = dto.DisplayOrder;
        category.ImageUrl = dto.ImageUrl;
        category.Icon = dto.Icon;
        category.Color = dto.Color;
        category.Status = dto.Status;
        category.CategoryType = dto.CategoryType;
        ApplyCategoryImage(category, imageBytes, imageFileName, imageContentType, replaceImage, replaceImage && (imageBytes == null || imageBytes.Length == 0));

        try
        {
            await _repository.SaveChangesAsync();
        }
        catch (Exception ex) when (IsDuplicateKeyException(ex))
        {
            throw new InvalidOperationException("Category name must be unique per branch.");
        }

        _cache.Remove(GetPosMenuCacheKey(dto.BusinessId, dto.BranchId));
        InvalidateCategoryListCache(dto.BusinessId, dto.BranchId);

        return category;
    }

    public async Task DeleteCategoryAsync(int id, int businessId, int branchId)
    {
        await EnsureBranchExistsAsync(businessId, branchId);

        var category = await _repository.GetCategoryAsync(id, businessId, branchId, includeItems: true);
        if (category == null || category.BranchId != branchId)
            throw new InvalidOperationException("Category not found.");

        if (category.MenuItems.Count > 0)
            throw new InvalidOperationException("Cannot delete category that still has products.");

        if (category.SubCategories.Count > 0)
            throw new InvalidOperationException("Cannot delete category that still has subcategories.");

        category.IsDeleted = true;
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(businessId, branchId));
        InvalidateCategoryListCache(businessId, branchId);
    }

    public async Task<PagedResultDto<SubCategoryDto>> GetSubCategoriesPagedAsync(
        int businessId,
        int branchId,
        int page,
        int pageSize,
        string? search = null,
        int? categoryId = null,
        bool? status = null)
    {
        if (businessId <= 0)
            throw new InvalidOperationException("BusinessId is required.");

        if (branchId < 0)
            throw new InvalidOperationException("BranchId is required.");

        if (branchId > 0)
        {
            await EnsureBranchExistsAsync(businessId, branchId);
        }

        if (categoryId.HasValue && branchId > 0)
        {
            await ValidateSubCategoryContextAsync(categoryId.Value, businessId, branchId);
        }

        var result = await _repository.GetSubCategoriesPagedAsync(businessId, branchId, page, pageSize, search, categoryId, status);

        return new PagedResultDto<SubCategoryDto>
        {
            Data = result.Data.Select(sc => MapSubCategoryDto(sc)).ToList(),
            TotalRecords = result.TotalRecords,
            TotalPages = result.TotalPages,
            CurrentPage = result.CurrentPage
        };
    }

    public async Task<ICollection<SubCategoryDto>> GetSubCategoriesAsync(int businessId, int branchId, int? categoryId = null)
    {
        await EnsureBranchExistsAsync(businessId, branchId);

        if (categoryId.HasValue)
        {
            await ValidateSubCategoryContextAsync(categoryId.Value, businessId, branchId);
        }

        var subCategories = await _repository.GetSubCategoriesByBranchAsync(businessId, branchId, categoryId);
        return subCategories.Select(sc => MapSubCategoryDto(sc)).ToList();
    }

    public async Task<SubCategoryDto?> GetSubCategoryByIdAsync(int id, int businessId, int branchId, bool includeImage = false)
    {
        await EnsureBranchExistsAsync(businessId, branchId);

        var subCategory = await _repository.GetSubCategoryAsync(id, businessId, branchId);
        if (subCategory == null || subCategory.BranchId != branchId)
            return null;

        return MapSubCategoryDto(subCategory, includeImage);
    }

    public async Task<SubCategoryImageDto?> GetSubCategoryImageAsync(int id, int businessId, int branchId)
    {
        var subCategory = await _repository.GetSubCategoryAsync(id, businessId, branchId);
        if (subCategory == null || subCategory.ImageData == null || subCategory.ImageData.Length == 0)
            return null;

        return new SubCategoryImageDto
        {
            ImageData = subCategory.ImageData,
            ImageContentType = subCategory.ImageContentType ?? "application/octet-stream"
        };
    }

    public async Task<SubCategory> AddSubCategoryAsync(CreateSubCategoryDto dto, byte[]? imageBytes = null, string? imageContentType = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("SubCategory name is required.");

        await EnsureBranchExistsAsync(dto.BusinessId, dto.BranchId);
        await ValidateSubCategoryContextAsync(dto.CategoryId, dto.BusinessId, dto.BranchId);

        var duplicate = await _repository.GetSubCategoryByNameAsync(dto.Name, dto.CategoryId, dto.BusinessId, dto.BranchId);
        if (duplicate != null)
            throw new InvalidOperationException("SubCategory name must be unique within the selected category.");

        if (!string.IsNullOrWhiteSpace(dto.Code) &&
            await _repository.SubCategoryCodeExistsAsync(dto.Code, dto.BusinessId, dto.BranchId))
            throw new InvalidOperationException("SubCategory code must be unique per branch.");

        var resolvedCode = await ResolveSubCategoryCodeAsync(dto.Code, dto.BranchId);

        var subCategory = new SubCategory
        {
            Name = dto.Name.Trim(),
            Code = resolvedCode,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            Status = dto.Status,
            Icon = dto.Icon,
            CategoryId = dto.CategoryId,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId
        };

        ApplySubCategoryImage(subCategory, imageBytes, imageContentType, imageBytes != null && imageBytes.Length > 0, false);

        await _repository.AddSubCategoryAsync(subCategory);
        await _repository.SaveChangesAsync();
        InvalidateSubCategoryCaches(dto.BusinessId, dto.BranchId);

        return subCategory;
    }

    public async Task<SubCategory> UpdateSubCategoryAsync(
        int id,
        UpdateSubCategoryDto dto,
        byte[]? imageBytes = null,
        string? imageContentType = null,
        bool replaceImage = false)
    {
        var subCategory = await _repository.GetSubCategoryAsync(id, dto.BusinessId, dto.BranchId);
        if (subCategory == null)
            throw new InvalidOperationException("SubCategory not found.");

        await EnsureBranchExistsAsync(dto.BusinessId, dto.BranchId);

        if (subCategory.BranchId != dto.BranchId)
            throw new InvalidOperationException("SubCategory branch mismatch.");

        await ValidateSubCategoryContextAsync(dto.CategoryId, dto.BusinessId, dto.BranchId);

        var duplicate = await _repository.GetSubCategoryByNameAsync(dto.Name, dto.CategoryId, dto.BusinessId, dto.BranchId, id);
        if (duplicate != null)
            throw new InvalidOperationException("SubCategory name must be unique within the selected category.");

        if (!string.IsNullOrWhiteSpace(dto.Code) &&
            await _repository.SubCategoryCodeExistsAsync(dto.Code, dto.BusinessId, dto.BranchId, id))
            throw new InvalidOperationException("SubCategory code must be unique per branch.");

        subCategory.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Code))
            subCategory.Code = dto.Code.Trim();
        subCategory.Description = dto.Description;
        subCategory.DisplayOrder = dto.DisplayOrder;
        subCategory.Status = dto.Status;
        subCategory.Icon = dto.Icon;
        subCategory.CategoryId = dto.CategoryId;

        ApplySubCategoryImage(
            subCategory,
            imageBytes,
            imageContentType,
            replaceImage,
            replaceImage && (imageBytes == null || imageBytes.Length == 0));

        await _repository.SaveChangesAsync();
        InvalidateSubCategoryCaches(dto.BusinessId, dto.BranchId);

        return subCategory;
    }

    public async Task PatchSubCategoryStatusAsync(SubCategoryStatusPatchDto dto)
    {
        if (dto.Items.Count == 0)
            throw new InvalidOperationException("At least one subcategory status update is required.");

        await EnsureBranchExistsAsync(dto.BusinessId, dto.BranchId);

        foreach (var item in dto.Items)
        {
            var subCategory = await _repository.GetSubCategoryAsync(item.Id, dto.BusinessId, dto.BranchId);
            if (subCategory == null)
                throw new InvalidOperationException($"SubCategory {item.Id} not found.");

            subCategory.Status = item.Status;
        }

        await _repository.SaveChangesAsync();
        InvalidateSubCategoryCaches(dto.BusinessId, dto.BranchId);
    }

    public async Task DeleteSubCategoryAsync(int id, int businessId, int branchId)
    {
        await EnsureBranchExistsAsync(businessId, branchId);

        var subCategory = await _repository.GetSubCategoryAsync(id, businessId, branchId);
        if (subCategory == null || subCategory.BranchId != branchId)
            throw new InvalidOperationException("SubCategory not found.");

        var hasProducts = await _repository.SubCategoryHasProductsAsync(id, businessId, branchId);
        if (hasProducts)
            throw new InvalidOperationException("Cannot delete subcategory that is used in products.");

        subCategory.IsDeleted = true;
        await _repository.SaveChangesAsync();
        InvalidateSubCategoryCaches(businessId, branchId);
    }

    public async Task<ICollection<MenuItemDto>> GetMenuItemsAsync(int businessId, int branchId, ProductType? productType = null, bool? isSaleable = null, bool? isInventoryItem = null)
    {
        var items = await _repository.GetMenuItemsByBranchAsync(businessId, branchId, productType, isSaleable, isInventoryItem);
        return items.Select(MapMenuItemDto).ToList();
    }

    public async Task<MenuItemDto?> GetMenuItemByIdAsync(int id, int businessId, int branchId)
    {
        var item = await _repository.GetMenuItemAsync(id, businessId, branchId, includeOptions: true);
        if (item == null || item.BranchId != branchId)
            return null;

        return MapMenuItemDto(item);
    }

    public async Task<MenuItem> AddMenuItemAsync(CreateMenuItemDto dto)
    {
        var category = await _repository.GetCategoryAsync(dto.MenuCategoryId, dto.BusinessId, dto.BranchId);
        if (category == null)
            throw new InvalidOperationException("Menu category not found.");

        if (category.BranchId != dto.BranchId)
            throw new InvalidOperationException("Category branch mismatch.");

        ValidateCategoryProductCompatibility(category.CategoryType, dto.ProductType);

        var expectedFlags = ResolveFlagsByProductType(dto.ProductType);
        ValidateOptionalFlags(dto.ProductType, dto.IsSaleable, dto.IsInventoryItem, dto.IsRecipeItem, dto.IsPurchasable, expectedFlags);

        var menuItem = new MenuItem
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            TaxPercentage = dto.Tax,
            PreparationTime = dto.PreparationTime,
            MenuCategoryId = dto.MenuCategoryId,
            BusinessId = dto.BusinessId,
            BranchId = dto.BranchId,
            ProductType = dto.ProductType,
            IsSaleable = expectedFlags.isSaleable,
            IsInventoryItem = expectedFlags.isInventoryItem,
            IsRecipeItem = expectedFlags.isRecipeItem,
            IsPurchasable = expectedFlags.isPurchasable,
            Variants = dto.Variants.Select(v => new MenuItemVariant
            {
                Name = v.Name,
                Price = v.Price,
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId
            }).ToList(),
            Addons = dto.Addons.Select(a => new MenuItemAddon
            {
                Name = a.Name,
                Price = a.Price,
                BusinessId = dto.BusinessId,
                BranchId = dto.BranchId
            }).ToList()
        };

        await _repository.AddMenuItemAsync(menuItem);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(dto.BusinessId, dto.BranchId));

        return menuItem;
    }

    public async Task<MenuItem> UpdateMenuItemAsync(int id, UpdateMenuItemDto dto)
    {
        var menuItem = await _repository.GetMenuItemAsync(id, dto.BusinessId, dto.BranchId, includeOptions: true);
        if (menuItem == null)
            throw new InvalidOperationException("Product not found.");

        if (menuItem.BranchId != dto.BranchId)
            throw new InvalidOperationException("Product branch mismatch.");

        var category = await _repository.GetCategoryAsync(dto.MenuCategoryId, dto.BusinessId, dto.BranchId);
        if (category == null)
            throw new InvalidOperationException("Menu category not found.");

        if (category.BranchId != dto.BranchId)
            throw new InvalidOperationException("Category branch mismatch.");

        ValidateCategoryProductCompatibility(category.CategoryType, dto.ProductType);

        var expectedFlags = ResolveFlagsByProductType(dto.ProductType);
        ValidateOptionalFlags(dto.ProductType, dto.IsSaleable, dto.IsInventoryItem, dto.IsRecipeItem, dto.IsPurchasable, expectedFlags);

        menuItem.Name = dto.Name;
        menuItem.Description = dto.Description;
        menuItem.Price = dto.Price;
        menuItem.TaxPercentage = dto.Tax;
        menuItem.PreparationTime = dto.PreparationTime;
        menuItem.MenuCategoryId = dto.MenuCategoryId;
        menuItem.ProductType = dto.ProductType;
        menuItem.IsSaleable = expectedFlags.isSaleable;
        menuItem.IsInventoryItem = expectedFlags.isInventoryItem;
        menuItem.IsRecipeItem = expectedFlags.isRecipeItem;
        menuItem.IsPurchasable = expectedFlags.isPurchasable;

        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(dto.BusinessId, dto.BranchId));

        return menuItem;
    }

    public async Task DeleteMenuItemAsync(int id, int businessId, int branchId)
    {
        var menuItem = await _repository.GetMenuItemAsync(id, businessId, branchId, includeOptions: true);
        if (menuItem == null || menuItem.BranchId != branchId)
            throw new InvalidOperationException("Product not found.");

        _repository.RemoveMenuItem(menuItem);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(businessId, branchId));
    }

    public async Task<MenuItemVariant> AddVariantAsync(CreateMenuItemVariantDto dto, int menuItemId, int businessId, int branchId)
    {
        var variant = new MenuItemVariant
        {
            Name = dto.Name,
            Price = dto.Price,
            MenuItemId = menuItemId,
            BusinessId = businessId,
            BranchId = branchId
        };

        await _repository.AddVariantAsync(variant);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(businessId, branchId));

        return variant;
    }

    public async Task<MenuItemAddon> AddAddonAsync(CreateMenuItemAddonDto dto, int menuItemId, int businessId, int branchId)
    {
        var addon = new MenuItemAddon
        {
            Name = dto.Name,
            Price = dto.Price,
            MenuItemId = menuItemId,
            BusinessId = businessId,
            BranchId = branchId
        };

        await _repository.AddAddonAsync(addon);
        await _repository.SaveChangesAsync();
        _cache.Remove(GetPosMenuCacheKey(businessId, branchId));

        return addon;
    }

    public async Task<MenuDto> GetFullMenuAsync(int businessId, int branchId)
    {
        var categories = await _repository.GetCategoriesWithItemsAsync(businessId, branchId);

        var menuDto = new MenuDto
        {
            Categories = categories.Select(MapCategoryDto).ToList()
        };

        return menuDto;
    }

    public async Task<MenuDto> GetPosMenuAsync(int businessId, int branchId)
    {
        if (_cache.TryGetValue(GetPosMenuCacheKey(businessId, branchId), out MenuDto? cachedMenu) && cachedMenu != null)
        {
            return cachedMenu;
        }

        var categories = await _repository.GetPosCategoriesWithItemsAsync(businessId, branchId);

        var menu = new MenuDto
        {
            Categories = categories.Select(MapCategoryDto).ToList()
        };

        _cache.Set(GetPosMenuCacheKey(businessId, branchId), menu, PosCacheDuration);

        return menu;
    }
}