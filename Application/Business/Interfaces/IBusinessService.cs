using POSSystem.Application.Business.DTOs;
using POSSystem.Application.Common.DTOs;

namespace POSSystem.Application.Business.Interfaces;

public interface IBusinessService
{
    Task<PagedResultDto<BusinessListItemDto>> GetBusinessesAsync(int page, int pageSize, string? search = null, string? sortBy = null, string? sortDirection = null);
    Task<BusinessDetailDto?> GetBusinessByIdAsync(int id);
    Task<BusinessLogoDto?> GetBusinessLogoAsync(int id);
    Task<BusinessDetailDto> CreateBusinessAsync(CreateBusinessDto dto, byte[]? logo, string? logoFileName, string? logoContentType);
    Task<BusinessDetailDto?> UpdateBusinessAsync(int id, UpdateBusinessDto dto, byte[]? logo, string? logoFileName, string? logoContentType, bool replaceLogo);
    Task<bool> DeleteBusinessAsync(int id);
}
