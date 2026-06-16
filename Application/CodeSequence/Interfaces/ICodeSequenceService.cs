using POSSystem.Application.CodeSequence.DTOs;

namespace POSSystem.Application.CodeSequence.Interfaces;

public interface ICodeSequenceService
{
    Task<IReadOnlyList<CodeSequenceListDto>> GetAllAsync(int? branchId = null, CancellationToken cancellationToken = default);
    Task<CodeSequenceListDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CodeSequenceListDto> UpdateLastNumberAsync(int id, UpdateCodeSequenceDto dto, CancellationToken cancellationToken = default);
}
