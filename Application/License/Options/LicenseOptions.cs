namespace POSSystem.Application.License.Options;

public sealed class LicenseOptions
{
    public const string SectionName = "License";

    public string Directory { get; set; } = "licenses";
    public string FileName { get; set; } = "system.lic";
    public string AesKeyBase64 { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// Used only by the external license generator tool. Never deploy to production servers.
    /// </summary>
    public string? PrivateKeyPem { get; set; }

    public bool AllowMissingInDevelopment { get; set; } = true;

    /// <summary>
    /// Default license validity in months. When greater than zero, this takes priority over DefaultValidityYears.
    /// </summary>
    public int DefaultValidityMonths { get; set; }

    /// <summary>
    /// Default license validity in years. Used when DefaultValidityMonths is zero.
    /// </summary>
    public int DefaultValidityYears { get; set; } = 10;
}
