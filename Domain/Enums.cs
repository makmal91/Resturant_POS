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
}

public enum CashFlowPaymentMethod
{
    Cash    = 1,
    Bank    = 2,
    Wallet  = 3,
}
