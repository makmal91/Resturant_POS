namespace POSSystem.Domain;

public enum ProductType
{
    RawMaterial,
    FinishedGood,
    SemiFinished,
    Service
}

public enum CategoryType
{
    Sale,
    Inventory
}

public enum OrderType
{
    DineIn,
    Takeaway,
    Delivery
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended,
    Terminated
}

public enum ShiftType
{
    Morning,
    Afternoon,
    Evening,
    Night,
    Flexible
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    InProgress,
    Served,
    Completed,
    Cancelled,
    Returned
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded,
    Voided
}

public enum TableStatus
{
    Available,
    Occupied,
    Reserved,
    Maintenance
}

public enum StockMovementType
{
    In,
    Out,
    Transfer
}

public enum PurchaseStatus
{
    Draft = 0,
    Posted = 1,
    Cancelled = 2
}

public enum StockLedgerType
{
    PurchaseEntry    = 1,   // Purchase
    SaleEntry        = 2,   // Sale
    PurchaseReturn   = 3,   // Return In (purchase return)
    Adjustment       = 4,
    TransferOut      = 5,   // Return Out / transfer out
    TransferIn       = 6,   // Return In / transfer in
    SaleReturn       = 7,   // Return In (sale return)
    SaleReversal     = 8,
    PurchaseReversal = 9,
    Opening          = 10,
    OpeningReversal  = 11,
}

public enum CashFlowTransactionType
{
    Sale            = 1,
    Expense         = 2,
    CashIn          = 3,
    CashOut         = 4,
    BankTransfer    = 5,
    OpeningBalance  = 6,
    ClosingBalance  = 7,
    Reversal        = 8,
}

public enum CashFlowPaymentMethod
{
    Cash    = 1,
    Bank    = 2,
    Wallet  = 3,
}

/// <summary>Chart of accounts classification (double-entry accounting).</summary>
public enum AccountType
{
    Asset     = 1,
    Liability = 2,
    Income    = 3,
    Expense   = 4,
    Equity    = 5,
}

/// <summary>Source document type for a general-ledger journal line.</summary>
public enum GlTransactionType
{
    Manual          = 0,
    Sale            = 1,
    Purchase        = 2,
    Payment         = 3,
    Receipt         = 4,
    Expense         = 5,
    OpeningBalance  = 6,
    Adjustment      = 7,
    Reversal        = 8,
    OpeningStockVoucher = 9,
}
