using Microsoft.EntityFrameworkCore;
using POSSystem.Application.CodeSequence.DTOs;
using POSSystem.Application.CodeSequence.Interfaces;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;
using POSSystem.Infrastructure.Services;

namespace POSSystem.Infrastructure.Services;

public class CodeSequenceService : ICodeSequenceService
{
    private readonly POSDbContext _context;
    private readonly ICodeGeneratorService _codeGenerator;

    public CodeSequenceService(POSDbContext context, ICodeGeneratorService codeGenerator)
    {
        _context = context;
        _codeGenerator = codeGenerator;
    }

    public async Task<IReadOnlyList<CodeSequenceListDto>> GetAllAsync(int? branchId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CodeSequences.AsNoTracking().AsQueryable();

        if (branchId.HasValue && branchId.Value > 0)
            query = query.Where(s => s.BranchId == branchId.Value);

        var sequences = await query
            .OrderBy(s => s.ModuleName)
            .ThenBy(s => s.BranchId)
            .ToListAsync(cancellationToken);

        var branchIds = sequences
            .Where(s => s.BranchId.HasValue)
            .Select(s => s.BranchId!.Value)
            .Distinct()
            .ToList();

        var branchNames = branchIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.Branches
                .AsNoTracking()
                .Where(b => branchIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken);

        var result = new List<CodeSequenceListDto>();
        foreach (var sequence in sequences)
        {
            var preview = await _codeGenerator.PreviewAsync(sequence.ModuleName, sequence.BranchId, cancellationToken);
            result.Add(MapDto(sequence, preview, branchNames));
        }

        return result;
    }

    public async Task<CodeSequenceListDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sequence = await _context.CodeSequences.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sequence == null)
            return null;

        string? branchName = null;
        if (sequence.BranchId.HasValue)
        {
            branchName = await _context.Branches
                .AsNoTracking()
                .Where(b => b.Id == sequence.BranchId.Value)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var preview = await _codeGenerator.PreviewAsync(sequence.ModuleName, sequence.BranchId, cancellationToken);
        return MapDto(sequence, preview, sequence.BranchId.HasValue && branchName != null
            ? new Dictionary<int, string> { [sequence.BranchId.Value] = branchName }
            : new Dictionary<int, string>());
    }

    public async Task<CodeSequenceListDto> UpdateLastNumberAsync(int id, UpdateCodeSequenceDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.LastNumber < 0)
            throw new InvalidOperationException("LastNumber cannot be negative.");

        var sequence = await _context.CodeSequences.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sequence == null)
            throw new InvalidOperationException("Code sequence not found.");

        sequence.LastNumber = dto.LastNumber;
        sequence.LastResetDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var updated = await GetByIdAsync(id, cancellationToken);
        return updated!;
    }

    private static CodeSequenceListDto MapDto(
        CodeSequence sequence,
        string nextPreview,
        IReadOnlyDictionary<int, string> branchNames)
    {
        string? branchName = null;
        if (sequence.BranchId.HasValue && branchNames.TryGetValue(sequence.BranchId.Value, out var name))
            branchName = name;

        return new CodeSequenceListDto
        {
            Id = sequence.Id,
            ModuleName = sequence.ModuleName,
            BranchId = sequence.BranchId,
            BranchName = branchName ?? (sequence.BranchId.HasValue ? $"Branch #{sequence.BranchId}" : "Global"),
            Prefix = sequence.Prefix,
            LastNumber = sequence.LastNumber,
            NextCodePreview = nextPreview,
            ResetType = sequence.ResetType.ToString(),
            LastResetDate = sequence.LastResetDate
        };
    }
}
