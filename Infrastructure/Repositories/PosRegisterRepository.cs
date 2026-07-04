using Microsoft.EntityFrameworkCore;
using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Repositories;

public class PosRegisterRepository : IPosRegisterRepository
{
    private readonly POSDbContext _db;

    public PosRegisterRepository(POSDbContext db) => _db = db;

    public async Task<IReadOnlyList<PosRegister>> GetRegistersAsync(int branchId) =>
        await _db.PosRegisters
            .AsNoTracking()
            .Include(r => r.LinkedCashAccount)
            .Where(r => !r.IsDeleted && r.BranchId == branchId)
            .OrderByDescending(r => r.IsDefault)
            .ThenBy(r => r.Name)
            .ToListAsync();

    public async Task<PosRegister?> GetRegisterAsync(int registerId, int branchId) =>
        await _db.PosRegisters
            .Include(r => r.LinkedCashAccount)
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.Id == registerId && r.BranchId == branchId);

    public async Task<PosRegister?> GetDefaultRegisterAsync(int branchId) =>
        await _db.PosRegisters
            .Include(r => r.LinkedCashAccount)
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.BranchId == branchId && r.IsDefault);

    public async Task<RegisterSession?> GetOpenSessionAsync(int registerId) =>
        await _db.RegisterSessions
            .Include(s => s.PosRegister)
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.PosRegisterId == registerId && !s.IsClosed);

    public async Task<RegisterSession?> GetSessionForDateAsync(int registerId, DateTime date)
    {
        var d = date.Date;
        return await _db.RegisterSessions
            .Include(s => s.PosRegister)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.PosRegisterId == registerId && s.SessionDate == d);
    }

    public async Task<RegisterSession?> GetLastClosedSessionAsync(int registerId) =>
        await _db.RegisterSessions
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.PosRegisterId == registerId && s.IsClosed)
            .OrderByDescending(s => s.ClosedAt)
            .ThenByDescending(s => s.SessionDate)
            .FirstOrDefaultAsync();

    public async Task<RegisterSession?> GetSessionByIdAsync(int sessionId, int branchId) =>
        await _db.RegisterSessions
            .Include(s => s.PosRegister)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == sessionId && s.BranchId == branchId);

    public async Task AddRegisterAsync(PosRegister register)
    {
        _db.PosRegisters.Add(register);
        await _db.SaveChangesAsync();
    }

    public async Task AddSessionAsync(RegisterSession session)
    {
        _db.RegisterSessions.Add(session);
        await _db.SaveChangesAsync();
    }

    public async Task<RegisterSession?> GetOpenSessionForUpdateAsync(int registerId, int branchId) =>
        await _db.RegisterSessions
            .Include(s => s.PosRegister)
            .FirstOrDefaultAsync(s =>
                !s.IsDeleted && s.PosRegisterId == registerId && s.BranchId == branchId && !s.IsClosed);

    public async Task UpdateRegisterMasterAsync(PosRegister register)
    {
        _db.PosRegisters.Update(register);
        await _db.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task<(IReadOnlyList<RegisterSession> Items, int Total)> GetHistoryAsync(
        int branchId, RegisterHistoryFilter filter)
    {
        var query = _db.RegisterSessions
            .AsNoTracking()
            .Include(s => s.PosRegister)
            .Where(s => !s.IsDeleted && s.BranchId == branchId && s.IsClosed);

        if (filter.PosRegisterId is > 0)
            query = query.Where(s => s.PosRegisterId == filter.PosRegisterId);

        if (filter.From.HasValue)
            query = query.Where(s => s.SessionDate >= filter.From.Value.Date);

        if (filter.To.HasValue)
            query = query.Where(s => s.SessionDate <= filter.To.Value.Date);

        var total = await query.CountAsync();
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        var items = await query
            .OrderByDescending(s => s.SessionDate)
            .ThenByDescending(s => s.ClosedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<IReadOnlyList<RegisterSession>> GetOpenSessionsForBranchAsync(int branchId) =>
        await _db.RegisterSessions
            .AsNoTracking()
            .Include(s => s.PosRegister)
            .Where(s => !s.IsDeleted && s.BranchId == branchId && !s.IsClosed)
            .ToListAsync();

    public async Task<IReadOnlyDictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        return await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Username ?? $"User {u.Id}");
    }

    public async Task<(int BusinessId, string Name)?> GetBranchInfoAsync(int branchId)
    {
        var row = await _db.Branches.AsNoTracking()
            .Where(b => b.Id == branchId && !b.IsDeleted)
            .Select(b => new { b.BusinessId, b.Name })
            .FirstOrDefaultAsync();

        return row == null ? null : (row.BusinessId, row.Name);
    }

    public Task<bool> IsValidCashGlAccountAsync(int accountId) =>
        _db.GlAccounts.AsNoTracking()
            .AnyAsync(a => a.Id == accountId && !a.IsDeleted && a.Type == AccountType.Asset);
}
