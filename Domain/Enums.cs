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
