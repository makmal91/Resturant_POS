using POSSystem.Application.Barcode.DTOs;
using POSSystem.Application.Common.DTOs;
using ProductEntity = POSSystem.Domain.Product;

namespace POSSystem.Application.Barcode.Interfaces;

public interface IBarcodePrintRepository
{
    Task<PagedResultDto<ProductEntity>> SearchProductsAsync(BarcodePrintSearchRequestDto request);
    Task<Dictionary<int, decimal>> GetProductStockTotalsAsync(int businessId, int branchId, IEnumerable<int> productIds);
}
