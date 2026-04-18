using System;

namespace POSSystem.Domain;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public virtual Branch Branch { get; set; } = null!;
    public virtual Order Order { get; set; } = null!;
}
