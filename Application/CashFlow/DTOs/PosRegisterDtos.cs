namespace POSSystem.Application.CashFlow.DTOs;

public class PosRegisterDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LinkedCashAccountId { get; set; }
    public string LinkedCashAccountName { get; set; } = string.Empty;
    public string LinkedCashAccountCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool HasOpenSession { get; set; }
    public decimal? CurrentBalance { get; set; }
}

public class RegisterSessionDto
{
    public int Id { get; set; }
    public int PosRegisterId { get; set; }
    public string RegisterName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public DateTime SessionDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsOpeningOverride { get; set; }
    public string? OpeningOverrideReason { get; set; }
    public int? OpenedBy { get; set; }
    public string? OpenedByName { get; set; }
    public DateTime OpenedAt { get; set; }
    public decimal? ExpectedClosing { get; set; }
    public decimal? PhysicalCash { get; set; }
    public decimal? Difference { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalExpensesCash { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalAdjustments { get; set; }
    public bool IsClosed { get; set; }
    public int? ClosedBy { get; set; }
    public string? ClosedByName { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CloseMismatchReason { get; set; }
    public string? Notes { get; set; }
}

public class RegisterOpeningHintDto
{
    public int PosRegisterId { get; set; }
    public string RegisterName { get; set; } = string.Empty;
    public bool IsFirstTime { get; set; }
    public decimal? LastClosingBalance { get; set; }
    public DateTime? LastClosedAt { get; set; }
    public decimal SuggestedOpeningBalance { get; set; }
    public bool HasOpenSessionToday { get; set; }
    public RegisterSessionDto? OpenSession { get; set; }
}

public class OpenRegisterRequest
{
    public int PosRegisterId { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool OverrideOpening { get; set; }
    public string? OverrideReason { get; set; }
}

public class CloseRegisterRequest
{
    public int PosRegisterId { get; set; }
    public decimal PhysicalCash { get; set; }
    public string? MismatchReason { get; set; }
    public string? Notes { get; set; }
}

public class RegisterClosePreviewDto
{
    public int PosRegisterId { get; set; }
    public string RegisterName { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalExpensesCash { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalAdjustments { get; set; }
    public decimal ExpectedCash { get; set; }
    public bool IsClosed { get; set; }
}

public class CreatePosRegisterRequest
{
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LinkedCashAccountId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePosRegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public int LinkedCashAccountId { get; set; }
    public bool IsActive { get; set; }
}

public class RegisterHistoryFilter
{
    public int? PosRegisterId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class RegisterHistoryPageDto
{
    public IReadOnlyList<RegisterSessionDto> Items { get; set; } = Array.Empty<RegisterSessionDto>();
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
}

public class RegisterDashboardDto
{
    public IReadOnlyList<PosRegisterDto> Registers { get; set; } = Array.Empty<PosRegisterDto>();
    public IReadOnlyList<RegisterSessionDto> OpenSessions { get; set; } = Array.Empty<RegisterSessionDto>();
}
