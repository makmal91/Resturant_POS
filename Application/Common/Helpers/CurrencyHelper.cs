namespace POSSystem.Application.Common.Helpers;

public static class CurrencyHelper
{
    public const string BaseCurrencyCode = "PKR";

    public static string GetSymbol(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return "₨";

        return currencyCode.Trim().ToUpperInvariant() switch
        {
            "PKR" => "₨",
            "USD" => "$",
            "GBP" => "£",
            "EUR" => "€",
            "AED" => "د.إ",
            "SAR" => "﷼",
            _ => currencyCode.Trim().ToUpperInvariant()
        };
    }

    public static decimal ToPKR(decimal amount, decimal exchangeRateToPKR)
    {
        if (exchangeRateToPKR <= 0)
            return amount;

        return Math.Round(amount * exchangeRateToPKR, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal FromPKR(decimal amountInPKR, decimal exchangeRateToPKR)
    {
        if (exchangeRateToPKR <= 0)
            return amountInPKR;

        return Math.Round(amountInPKR / exchangeRateToPKR, 2, MidpointRounding.AwayFromZero);
    }
}
