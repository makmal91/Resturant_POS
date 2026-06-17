namespace POSSystem.Application.License.Models;

public sealed class LicenseSnapshot
{
    public LicensePayload Payload { get; init; } = new();
    public DateTime LoadedAt { get; init; }
    public bool IsValid { get; init; }
    public bool IsExpired { get; init; }
    public string? ValidationMessage { get; init; }
}
