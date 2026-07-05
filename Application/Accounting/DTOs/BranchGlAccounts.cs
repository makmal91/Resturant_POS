namespace POSSystem.Application.Accounting.DTOs;

public sealed class BranchGlAccounts
{
    public int Cash { get; init; }
    public int Bank { get; init; }
    public int Inventory { get; init; }
    public int OwnerCapital { get; init; }
    public int Sales { get; init; }
    public int GeneralExpense { get; init; }
    public int? CostOfGoodsSold { get; init; }
}
