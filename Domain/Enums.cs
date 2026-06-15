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
    PurchaseEntry    = 1,   // Stock in from a supplier purchase
    SaleEntry        = 2,   // Stock out from a customer sale
    PurchaseReturn   = 3,   // Stock returned back to supplier
    Adjustment       = 4,   // Manual stock adjustment
    TransferOut      = 5,   // Stock moved out of a warehouse
    TransferIn       = 6,   // Stock received into a warehouse
    SaleReturn       = 7,   // Stock returned by a customer
}
