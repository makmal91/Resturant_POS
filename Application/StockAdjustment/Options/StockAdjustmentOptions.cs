namespace POSSystem.Application.StockAdjustment.Options;

public class StockAdjustmentOptions
{
    public const string SectionName = "StockAdjustment";

    public bool EnableStockAdjustmentAccounting { get; set; } = true;
    public bool AllowNegativeStock { get; set; } = false;
}
