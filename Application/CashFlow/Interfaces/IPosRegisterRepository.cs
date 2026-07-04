using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.CashFlow.Interfaces;

public interface IPosRegisterRepository
{
    Task<IReadOnlyList<PosRegister>> GetRegistersAsync(int branchId);
    Task<PosRegister?> GetRegisterAsync(int registerId, int branchId);
    Task<PosRegister?> GetDefaultRegisterAsync(int branchId);
    Task<RegisterSession?> GetOpenSessionAsync(int registerId);
    Task<RegisterSession?> GetSessionForDateAsync(int registerId, DateTime date);
    Task<RegisterSession?> GetLastClosedSessionAsync(int registerId);
    Task<RegisterSession?> GetSessionByIdAsync(int sessionId, int branchId);
    Task AddRegisterAsync(PosRegister register);
    Task AddSessionAsync(RegisterSession session);
    Task<RegisterSession?> GetOpenSessionForUpdateAsync(int registerId, int branchId);
    Task UpdateRegisterMasterAsync(PosRegister register);
    Task SaveChangesAsync();
    Task<(IReadOnlyList<RegisterSession> Items, int Total)> GetHistoryAsync(
        int branchId, RegisterHistoryFilter filter);
    Task<IReadOnlyList<RegisterSession>> GetOpenSessionsForBranchAsync(int branchId);
    Task<IReadOnlyDictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds);
    Task<(int BusinessId, string Name)?> GetBranchInfoAsync(int branchId);
    Task<bool> IsValidCashGlAccountAsync(int accountId);
}
