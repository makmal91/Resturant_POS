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
    PurchaseEntry    = 1,
    SaleEntry        = 2,
    PurchaseReturn   = 3,
    Adjustment       = 4,
    TransferOut      = 5,
    TransferIn       = 6,
    SaleReturn       = 7,
    SaleReversal     = 8,
    PurchaseReversal = 9,
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
