namespace POSSystem.Domain;

public class Currency
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal ExchangeRateToPKR { get; set; } = 1m;
    public bool IsBase { get; set; }
    public bool IsActive { get; set; } = true;
}
