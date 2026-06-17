using POSSystem.Application.License.Options;

namespace POSSystem.Application.License;

public static class LicenseValidity
{
    public static DateTime ResolveExpiresAt(
        DateTime issuedAt,
        int? monthsOverride,
        int? yearsOverride,
        LicenseOptions options)
    {
        if (monthsOverride is > 0)
            return issuedAt.AddMonths(monthsOverride.Value);

        if (yearsOverride is > 0)
            return issuedAt.AddYears(yearsOverride.Value);

        if (options.DefaultValidityMonths > 0)
            return issuedAt.AddMonths(options.DefaultValidityMonths);

        var years = options.DefaultValidityYears > 0 ? options.DefaultValidityYears : 10;
        return issuedAt.AddYears(years);
    }

    public static string DescribePeriod(int? monthsOverride, int? yearsOverride, LicenseOptions options)
    {
        if (monthsOverride is > 0)
            return monthsOverride.Value == 1 ? "1 month" : $"{monthsOverride.Value} months";

        if (yearsOverride is > 0)
            return yearsOverride.Value == 1 ? "1 year" : $"{yearsOverride.Value} years";

        if (options.DefaultValidityMonths > 0)
            return options.DefaultValidityMonths == 1
                ? "1 month (default)"
                : $"{options.DefaultValidityMonths} months (default)";

        var years = options.DefaultValidityYears > 0 ? options.DefaultValidityYears : 10;
        return years == 1 ? "1 year (default)" : $"{years} years (default)";
    }
}
