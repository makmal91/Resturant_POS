namespace POSSystem.Application.Accounting.DTOs;

public sealed record PartyGlLinkRow(int PartyId, int BusinessId, int BranchId, string Name, string Code);
