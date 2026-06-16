namespace POSSystem.Application.Common.Interfaces;

public interface ICodeGeneratorService
{
    /// <summary>
    /// Reserves the next code by updating the tracked sequence entity.
    /// Does not persist — caller must SaveChanges in the same DbContext scope.
    /// </summary>
    Task<string> GenerateAsync(string moduleName, int? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns the next code without consuming the sequence (for UI preview).</summary>
    Task<string> PreviewAsync(string moduleName, int? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a code for entity creation: generates when empty, consumes the sequence when the
    /// submitted value matches the next preview, otherwise syncs the sequence for auto-formatted codes.
    /// Does not persist — caller must SaveChanges in the same DbContext scope.
    /// </summary>
    Task<string> ResolveAsync(string moduleName, int? branchId, string? requestedCode, CancellationToken cancellationToken = default);

    /// <summary>Generates a random unique 13-digit EAN-like barcode.</summary>
    Task<string> GenerateBarcodeAsync(int businessId, int branchId, CancellationToken cancellationToken = default);
}
