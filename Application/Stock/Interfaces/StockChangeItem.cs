namespace POSSystem.Application.Stock.Interfaces;

public record StockChangeItem(int ProductId, int? VariantId, int WarehouseId);
