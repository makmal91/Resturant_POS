namespace POSSystem.Application.Common.Interfaces;

public interface ICodeGeneratorService
{
    /// <summary>Increments the sequence and returns the next formatted code.</summary>
    Task<string> GenerateAsync(string moduleName, int? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns the next code without consuming the sequence (for UI preview).</summary>
    Task<string> PreviewAsync(string moduleName, int? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>Generates a random unique 13-digit EAN-like barcode.</summary>
    Task<string> GenerateBarcodeAsync(int businessId, int branchId, CancellationToken cancellationToken = default);
}
