using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POSSystem.API.Authorization;
using POSSystem.API.Extensions;
using POSSystem.Application.CodeSequence.DTOs;
using POSSystem.Application.CodeSequence.Interfaces;
using POSSystem.Application.Common.Constants;
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

        var effectiveBranchId = this.ResolveEffectiveBranchId(branchId);

        var code = await _codeGenerator.PreviewAsync(module, effectiveBranchId);
        return Ok(new { code });
    }

    /// <summary>Generate a random unique EAN-like barcode.</summary>
    [HttpPost("barcode")]
    public async Task<IActionResult> GenerateBarcode([FromQuery] int? branchId, [FromQuery] int? businessId)
    {
        var resolvedBusinessId = this.ResolveBusinessId(businessId);
        var resolvedBranchId = this.ResolveEffectiveBranchId(branchId);

        if (!resolvedBranchId.HasValue || resolvedBranchId.Value <= 0)
            return BadRequest(new { message = "BranchId is required." });

        var barcode = await _codeGenerator.GenerateBarcodeAsync(resolvedBusinessId, resolvedBranchId.Value);
        return Ok(new { barcode });
    }
}

[Authorize]
[ApiController]
[Route("api/code-sequences")]
public class CodeSequencesController : ControllerBase
{
    private readonly ICodeSequenceService _codeSequenceService;

    public CodeSequencesController(ICodeSequenceService codeSequenceService)
        => _codeSequenceService = codeSequenceService;

    [HttpGet]
    [RequirePermission(PermissionModules.CodeSequences, PermissionActions.View)]
    public async Task<IActionResult> GetAll([FromQuery] int? branchId)
    {
        var items = await _codeSequenceService.GetAllAsync(this.ResolveEffectiveBranchId(branchId));
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionModules.CodeSequences, PermissionActions.View)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _codeSequenceService.GetByIdAsync(id);
        return item == null ? NotFound(new { message = "Code sequence not found." }) : Ok(item);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionModules.CodeSequences, PermissionActions.Edit)]
    public async Task<IActionResult> UpdateLastNumber(int id, [FromBody] UpdateCodeSequenceDto dto)
    {
        try
        {
            var updated = await _codeSequenceService.UpdateLastNumberAsync(id, dto);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
