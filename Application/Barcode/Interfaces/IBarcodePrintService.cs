using POSSystem.Application.Barcode.DTOs;
using POSSystem.Application.Common.DTOs;

namespace POSSystem.Application.Barcode.Interfaces;

public interface IBarcodePrintService
{
    Task<PagedResultDto<BarcodePrintProductDto>> SearchItemsAsync(BarcodePrintSearchRequestDto request);
    Task<ProductPrintDetailsDto?> GetProductPrintDetailsAsync(int productId, int businessId, int branchId);
}
