using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.CashFlow.DTOs;
using POSSystem.Application.CashFlow.Interfaces;
using POSSystem.Domain;

namespace POSSystem.Application.CashFlow.Services;

public class PosRegisterService : IPosRegisterService
{
    private readonly IPosRegisterRepository _registers;
    private readonly ICashFlowRepository _cashFlow;
    private readonly IGlReportingRepository _glReporting;

    public PosRegisterService(
        IPosRegisterRepository registers,
        ICashFlowRepository cashFlow,
        IGlReportingRepository glReporting)
    {
        _registers = registers;
        _cashFlow = cashFlow;
        _glReporting = glReporting;
    }

    public async Task<RegisterDashboardDto> GetDashboardAsync(int branchId)
    {
        var registerRows = await _registers.GetRegistersAsync(branchId);
        var openSessions = await _registers.GetOpenSessionsForBranchAsync(branchId);

        var userNames = await _registers.GetUserNamesAsync(
            openSessions.SelectMany(s => new[] { s.OpenedBy, s.ClosedBy }).Where(id => id.HasValue).Select(id => id!.Value));

        var now = DateTime.UtcNow.AddSeconds(1);
        var liveByRegister = new Dictionary<int, GlCashDaySummary>();

        var registers = new List<PosRegisterDto>();
        foreach (var r in registerRows)
        {
            var open = openSessions.FirstOrDefault(s => s.PosRegisterId == r.Id);
            decimal? balance = null;
            if (open != null)
            {
                var gl = await _glReporting.GetGlCashAccountSessionSummaryAsync(
                    r.LinkedCashAccountId, branchId, open.OpenedAt, now);
                liveByRegister[r.Id] = gl;
                balance = open.OpeningBalance + gl.NetMovement;
            }

            registers.Add(MapRegister(r, open != null, balance));
        }

        var sessionDtos = openSessions.Select(s =>
        {
            var dto = MapSession(s, userNames);
            if (liveByRegister.TryGetValue(s.PosRegisterId, out var gl))
            {
                dto.TotalCashSales = gl.CashSales;
                dto.TotalExpensesCash = gl.Expenses;
                dto.TotalCashIn = gl.CashIn;
                dto.TotalCashOut = gl.CashOut;
                dto.TotalAdjustments = gl.CashIn - gl.CashOut;
                dto.ExpectedClosing = s.OpeningBalance + gl.NetMovement;
            }
            return dto;
        }).ToList();

        return new RegisterDashboardDto
        {
            Registers = registers,
            OpenSessions = sessionDtos,
        };
    }

    public async Task<IReadOnlyList<PosRegisterDto>> GetRegistersAsync(int branchId)
    {
        var rows = await _registers.GetRegistersAsync(branchId);
        var openIds = (await _registers.GetOpenSessionsForBranchAsync(branchId))
            .Select(s => s.PosRegisterId)
            .ToHashSet();

        return rows.Select(r => MapRegister(r, openIds.Contains(r.Id), null)).ToList();
    }

    public async Task<PosRegisterDto> CreateRegisterAsync(CreatePosRegisterRequest request, int userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Register name is required.");

        var branch = await _registers.GetBranchInfoAsync(request.BranchId)
            ?? throw new InvalidOperationException("Branch not found.");

        if (!await _registers.IsValidCashGlAccountAsync(request.LinkedCashAccountId))
            throw new InvalidOperationException("Valid cash GL account is required.");

        var register = new PosRegister
        {
            BusinessId = branch.BusinessId,
            BranchId = request.BranchId,
            Name = request.Name.Trim(),
            LinkedCashAccountId = request.LinkedCashAccountId,
            IsActive = request.IsActive,
            IsDefault = false,
            CreatedBy = userId,
        };

        await _registers.AddRegisterAsync(register);
        var saved = await _registers.GetRegisterAsync(register.Id, request.BranchId)
            ?? register;
        return MapRegister(saved, false, null);
    }

    public async Task<PosRegisterDto> UpdateRegisterAsync(
        int id, UpdatePosRegisterRequest request, int branchId, int userId)
    {
        var register = await _registers.GetRegisterAsync(id, branchId)
            ?? throw new InvalidOperationException("Register not found.");

        if (register.IsDefault && !request.IsActive)
            throw new InvalidOperationException("The default register cannot be deactivated.");

        if (!await _registers.IsValidCashGlAccountAsync(request.LinkedCashAccountId))
            throw new InvalidOperationException("Valid cash GL account is required.");

        register.Name = request.Name.Trim();
        register.LinkedCashAccountId = request.LinkedCashAccountId;
        register.IsActive = request.IsActive;
        register.ModifiedBy = userId;
        register.ModifiedAt = DateTime.UtcNow;

        await _registers.UpdateRegisterMasterAsync(register);
        return MapRegister(register, false, null);
    }

    public async Task<RegisterOpeningHintDto> GetOpeningHintAsync(int registerId, int branchId)
    {
        var register = await _registers.GetRegisterAsync(registerId, branchId)
            ?? throw new InvalidOperationException("Register not found.");

        var openSession = await _registers.GetOpenSessionAsync(registerId);
        var lastClosed = await _registers.GetLastClosedSessionAsync(registerId);
        var isFirstTime = lastClosed == null;

        var lastClosing = lastClosed?.PhysicalCash ?? lastClosed?.ExpectedClosing;
        var suggested = isFirstTime ? 0m : lastClosing ?? 0m;

        RegisterSessionDto? openDto = null;
        if (openSession != null)
        {
            var names = await _registers.GetUserNamesAsync(new[] { openSession.OpenedBy ?? 0 });
            openDto = MapSession(openSession, names);
        }

        return new RegisterOpeningHintDto
        {
            PosRegisterId = register.Id,
            RegisterName = register.Name,
            IsFirstTime = isFirstTime,
            LastClosingBalance = lastClosing,
            LastClosedAt = lastClosed?.ClosedAt,
            SuggestedOpeningBalance = suggested,
            HasOpenSessionToday = openSession != null,
            OpenSession = openDto,
        };
    }

    public async Task<RegisterSessionDto> OpenRegisterAsync(OpenRegisterRequest request, int branchId, int userId)
    {
        var register = await _registers.GetRegisterAsync(request.PosRegisterId, branchId)
            ?? throw new InvalidOperationException("Register not found.");

        if (!register.IsActive)
            throw new InvalidOperationException("This register is inactive.");

        var today = DateTime.UtcNow.Date;

        var openSession = await _registers.GetOpenSessionAsync(register.Id);
        if (openSession != null)
            throw new InvalidOperationException(
                "This register already has an open session. Close it before opening a new one.");

        var lastClosed = await _registers.GetLastClosedSessionAsync(register.Id);
        var isFirstTime = lastClosed == null;
        var suggested = isFirstTime ? 0m : (lastClosed!.PhysicalCash ?? lastClosed.ExpectedClosing ?? 0m);

        decimal openingBalance;
        var isOverride = request.OverrideOpening;

        if (isFirstTime)
        {
            if (request.OpeningBalance < 0)
                throw new InvalidOperationException("Opening balance cannot be negative.");
            openingBalance = request.OpeningBalance;
            isOverride = false;
        }
        else if (isOverride)
        {
            openingBalance = request.OpeningBalance;
            if (openingBalance != suggested && string.IsNullOrWhiteSpace(request.OverrideReason))
                throw new InvalidOperationException("Override reason is required when opening balance differs from last closing.");
        }
        else
        {
            openingBalance = suggested;
        }

        var session = new RegisterSession
        {
            BusinessId = register.BusinessId,
            BranchId = branchId,
            PosRegisterId = register.Id,
            SessionDate = today,
            OpeningBalance = openingBalance,
            IsOpeningOverride = isOverride,
            OpeningOverrideReason = isOverride ? request.OverrideReason?.Trim() : null,
            OpenedBy = userId,
            OpenedAt = DateTime.UtcNow,
            IsClosed = false,
            CreatedBy = userId,
        };

        await _registers.AddSessionAsync(session);

        if (register.IsDefault)
            await SyncLegacyOpenAsync(register.BusinessId, branchId, today, openingBalance);

        session.PosRegister = register;
        var names = await _registers.GetUserNamesAsync(new[] { userId });
        return MapSession(session, names);
    }

    public async Task<RegisterClosePreviewDto> GetClosePreviewAsync(int registerId, int branchId)
    {
        var session = await _registers.GetOpenSessionForUpdateAsync(registerId, branchId)
            ?? throw new InvalidOperationException("No open register session found.");

        var gl = await _glReporting.GetGlCashAccountSessionSummaryAsync(
            session.PosRegister.LinkedCashAccountId, branchId, session.OpenedAt, DateTime.UtcNow.AddSeconds(1));

        return BuildClosePreview(session, gl);
    }

    public async Task<RegisterSessionDto> CloseRegisterAsync(CloseRegisterRequest request, int branchId, int userId)
    {
        if (request.PhysicalCash < 0)
            throw new InvalidOperationException("Physical cash cannot be negative.");

        var session = await _registers.GetOpenSessionForUpdateAsync(request.PosRegisterId, branchId)
            ?? throw new InvalidOperationException("No open register session found.");

        var gl = await _glReporting.GetGlCashAccountSessionSummaryAsync(
            session.PosRegister.LinkedCashAccountId, branchId, session.OpenedAt, DateTime.UtcNow.AddSeconds(1));

        var preview = BuildClosePreview(session, gl);
        var diff = request.PhysicalCash - preview.ExpectedCash;

        if (diff != 0 && string.IsNullOrWhiteSpace(request.MismatchReason))
            throw new InvalidOperationException("Cash mismatch detected. Please provide a reason before closing.");

        session.ExpectedClosing = preview.ExpectedCash;
        session.PhysicalCash = request.PhysicalCash;
        session.Difference = diff;
        session.TotalCashSales = preview.TotalCashSales;
        session.TotalExpensesCash = preview.TotalExpensesCash;
        session.TotalCashIn = preview.TotalCashIn;
        session.TotalCashOut = preview.TotalCashOut;
        session.TotalAdjustments = preview.TotalAdjustments;
        session.CloseMismatchReason = diff != 0 ? request.MismatchReason?.Trim() : null;
        session.Notes = request.Notes?.Trim();
        session.IsClosed = true;
        session.ClosedBy = userId;
        session.ClosedAt = DateTime.UtcNow;
        session.ModifiedBy = userId;
        session.ModifiedAt = DateTime.UtcNow;

        await _registers.SaveChangesAsync();

        if (session.PosRegister.IsDefault)
            await SyncLegacyCloseAsync(session, request.PhysicalCash, preview.ExpectedCash, diff, request.Notes);

        var names = await _registers.GetUserNamesAsync(new[] { session.OpenedBy ?? 0, userId });
        return MapSession(session, names);
    }

    public async Task<RegisterHistoryPageDto> GetHistoryAsync(int branchId, RegisterHistoryFilter filter)
    {
        var (items, total) = await _registers.GetHistoryAsync(branchId, filter);
        var userIds = items.SelectMany(s => new[] { s.OpenedBy, s.ClosedBy }).Where(id => id.HasValue).Select(id => id!.Value);
        var names = await _registers.GetUserNamesAsync(userIds);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        return new RegisterHistoryPageDto
        {
            Items = items.Select(s => MapSession(s, names)).ToList(),
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        };
    }

    private static RegisterClosePreviewDto BuildClosePreview(RegisterSession session, GlCashDaySummary gl) =>
        new()
        {
            PosRegisterId = session.PosRegisterId,
            RegisterName = session.PosRegister.Name,
            SessionId = session.Id,
            OpeningBalance = session.OpeningBalance,
            TotalCashSales = gl.CashSales,
            TotalExpensesCash = gl.Expenses,
            TotalCashIn = gl.CashIn,
            TotalCashOut = gl.CashOut,
            TotalAdjustments = gl.CashIn - gl.CashOut,
            ExpectedCash = session.OpeningBalance + gl.NetMovement,
            IsClosed = session.IsClosed,
        };

    private async Task SyncLegacyOpenAsync(int businessId, int branchId, DateTime date, decimal opening)
    {
        var legacy = await _cashFlow.GetRegisterAsync(businessId, branchId, date);
        if (legacy != null) return;

        await _cashFlow.AddRegisterAsync(new CashRegister
        {
            BusinessId = businessId,
            BranchId = branchId,
            RegisterDate = date,
            OpeningCash = opening,
        });
    }

    private async Task SyncLegacyCloseAsync(
        RegisterSession session, decimal physical, decimal expected, decimal diff, string? notes)
    {
        var legacy = await _cashFlow.GetRegisterAsync(session.BusinessId, session.BranchId, session.SessionDate);
        if (legacy == null) return;

        await _cashFlow.UpdateRegisterAsync(new CashRegister
        {
            Id = legacy.Id,
            BusinessId = legacy.BusinessId,
            BranchId = legacy.BranchId,
            RegisterDate = legacy.RegisterDate,
            OpeningCash = legacy.OpeningCash,
            ClosingCash = physical,
            ExpectedCash = expected,
            ActualCash = physical,
            Difference = diff,
            IsClosed = true,
            Notes = notes ?? legacy.Notes,
            ClosedAt = DateTime.UtcNow,
            ClosedBy = session.ClosedBy,
        });
    }

    private static PosRegisterDto MapRegister(PosRegister r, bool hasOpen, decimal? balance) => new()
    {
        Id = r.Id,
        BranchId = r.BranchId,
        Name = r.Name,
        LinkedCashAccountId = r.LinkedCashAccountId,
        LinkedCashAccountName = r.LinkedCashAccount?.Name ?? string.Empty,
        LinkedCashAccountCode = string.Empty,
        IsActive = r.IsActive,
        IsDefault = r.IsDefault,
        HasOpenSession = hasOpen,
        CurrentBalance = balance,
    };

    private static RegisterSessionDto MapSession(RegisterSession s, IReadOnlyDictionary<int, string> names) => new()
    {
        Id = s.Id,
        PosRegisterId = s.PosRegisterId,
        RegisterName = s.PosRegister?.Name ?? string.Empty,
        BranchId = s.BranchId,
        SessionDate = s.SessionDate,
        OpeningBalance = s.OpeningBalance,
        IsOpeningOverride = s.IsOpeningOverride,
        OpeningOverrideReason = s.OpeningOverrideReason,
        OpenedBy = s.OpenedBy,
        OpenedByName = s.OpenedBy is > 0 && names.TryGetValue(s.OpenedBy.Value, out var on) ? on : null,
        OpenedAt = s.OpenedAt,
        ExpectedClosing = s.ExpectedClosing,
        PhysicalCash = s.PhysicalCash,
        Difference = s.Difference,
        TotalCashSales = s.TotalCashSales,
        TotalExpensesCash = s.TotalExpensesCash,
        TotalCashIn = s.TotalCashIn,
        TotalCashOut = s.TotalCashOut,
        TotalAdjustments = s.TotalAdjustments,
        IsClosed = s.IsClosed,
        ClosedBy = s.ClosedBy,
        ClosedByName = s.ClosedBy is > 0 && names.TryGetValue(s.ClosedBy.Value, out var cn) ? cn : null,
        ClosedAt = s.ClosedAt,
        CloseMismatchReason = s.CloseMismatchReason,
        Notes = s.Notes,
    };
}
