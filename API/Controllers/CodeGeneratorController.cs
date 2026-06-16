using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Extensions;
using POSSystem.Application.Common.Interfaces;

namespace POSSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/codes")]
public class CodeGeneratorController : ControllerBase
{
    private readonly ICodeGeneratorService _codeGenerator;

    public CodeGeneratorController(ICodeGeneratorService codeGenerator)
        => _codeGenerator = codeGenerator;

    /// <summary>Preview the next code without consuming the sequence.</summary>
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string module, [FromQuery] int? branchId)
    {
        if (string.IsNullOrWhiteSpace(module))
            return BadRequest(new { message = "Module name is required." });

        var code = await _codeGenerator.PreviewAsync(module, branchId);
        return Ok(new { code });
    }

    /// <summary>Generate and consume the next code from the sequence.</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromQuery] string module, [FromQuery] int? branchId)
    {
        if (string.IsNullOrWhiteSpace(module))
            return BadRequest(new { message = "Module name is required." });

        var code = await _codeGenerator.GenerateAsync(module, branchId);
        return Ok(new { code });
    }

    /// <summary>Generate a random unique EAN-like barcode.</summary>
    [HttpPost("barcode")]
    public async Task<IActionResult> GenerateBarcode([FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveBranchId(branchId);

        if (resolvedBranchId <= 0)
            return BadRequest(new { message = "BranchId is required." });

        var barcode = await _codeGenerator.GenerateBarcodeAsync(resolvedBusinessId, resolvedBranchId);
        return Ok(new { barcode });
    }
}
