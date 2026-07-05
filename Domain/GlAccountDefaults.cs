namespace POSSystem.Domain;

/// <summary>Well-known chart-of-accounts names and professional ERP hierarchy (per branch).</summary>
public static class GlAccountDefaults
{
    // Top-level groups
    public const string Assets = "Assets";
    public const string Liabilities = "Liabilities";
    public const string Equity = "Equity";
    public const string Income = "Income";
    public const string Expenses = "Expenses";

    // Current assets
    public const string CurrentAssets = "Current Assets";
    public const string Cash = "Cash";
    public const string Bank = "Bank";
    public const string Inventory = "Inventory";
    public const string AccountsReceivable = "Accounts Receivable";
    public const string Customers = "Customers";

    // Fixed assets
    public const string FixedAssets = "Fixed Assets";
    public const string FurnitureAndFixtures = "Furniture & Fixtures";
    public const string Equipment = "Equipment";
    public const string Vehicles = "Vehicles";
    public const string Computers = "Computers";

    // Liabilities
    public const string CurrentLiabilities = "Current Liabilities";
    public const string AccountsPayable = "Accounts Payable";
    public const string Suppliers = "Suppliers";
    public const string LongTermLiabilities = "Long-Term Liabilities";
    public const string LoansPayable = "Loans Payable";

    // Equity
    public const string OwnerCapital = "Owner Capital";
    public const string OpeningStock = "Opening Stock";
    public const string RetainedEarnings = "Retained Earnings";
    public const string Drawings = "Drawings";

    // Income & expenses
    public const string Sales = "Sales";
    public const string CostOfGoodsSold = "Cost of Goods Sold";
    public const string GeneralExpenses = "General Expenses";
    public const string GeneralExpense = "General Expense";
    public const string Rent = "Rent";
    public const string Salary = "Salary";
    public const string Utilities = "Utilities";

    /// <summary>Party sub-accounts are created under this folder (not directly under AR).</summary>
    public const string CustomerPartyParent = Customers;

    /// <summary>Party sub-accounts are created under this folder (not directly under AP).</summary>
    public const string SupplierPartyParent = Suppliers;

    /// <summary>Professional chart-of-accounts tree seeded idempotently per branch.</summary>
    public static readonly GlCoaSeedNode[] CoaHierarchy =
    [
        new(Assets, AccountType.Asset,
        [
            new(CurrentAssets, AccountType.Asset,
            [
                new(Cash, AccountType.Asset),
                new(Bank, AccountType.Asset),
                new(Inventory, AccountType.Asset),
                new(AccountsReceivable, AccountType.Asset,
                [
                    new(Customers, AccountType.Asset),
                ]),
            ]),
            new(FixedAssets, AccountType.Asset,
            [
                new(FurnitureAndFixtures, AccountType.Asset),
                new(Equipment, AccountType.Asset),
                new(Vehicles, AccountType.Asset),
                new(Computers, AccountType.Asset),
            ]),
        ]),
        new(Liabilities, AccountType.Liability,
        [
            new(CurrentLiabilities, AccountType.Liability,
            [
                new(AccountsPayable, AccountType.Liability,
                [
                    new(Suppliers, AccountType.Liability),
                ]),
            ]),
            new(LongTermLiabilities, AccountType.Liability,
            [
                new(LoansPayable, AccountType.Liability),
            ]),
        ]),
        new(Equity, AccountType.Equity,
        [
            new(OwnerCapital, AccountType.Equity),
            new(OpeningStock, AccountType.Equity),
            new(RetainedEarnings, AccountType.Equity),
            new(Drawings, AccountType.Equity),
        ]),
        new(Income, AccountType.Income,
        [
            new(Sales, AccountType.Income),
        ]),
        new(Expenses, AccountType.Expense,
        [
            new(CostOfGoodsSold, AccountType.Expense),
            new(GeneralExpenses, AccountType.Expense,
            [
                new(GeneralExpense, AccountType.Expense),
                new(Rent, AccountType.Expense),
                new(Salary, AccountType.Expense),
                new(Utilities, AccountType.Expense),
            ]),
        ]),
    ];

    /// <summary>Legacy flat seed list — superseded by <see cref="CoaHierarchy"/> but kept for name resolution.</summary>
    public static readonly (string Name, AccountType Type)[] SeedAccounts =
    [
        (Cash, AccountType.Asset),
        (Bank, AccountType.Asset),
        (Inventory, AccountType.Asset),
        (AccountsReceivable, AccountType.Asset),
        (AccountsPayable, AccountType.Liability),
        (Sales, AccountType.Income),
        (GeneralExpense, AccountType.Expense),
        (CostOfGoodsSold, AccountType.Expense),
    ];

    public static string FormatPartyAccountName(string partyName, string partyCode)
    {
        var name = $"{partyName.Trim()} [{partyCode.Trim()}]";
        return name.Length <= 200 ? name : name[..200];
    }
}

/// <summary>Node in the professional chart-of-accounts tree.</summary>
public sealed record GlCoaSeedNode(string Name, AccountType Type, GlCoaSeedNode[]? Children = null);
