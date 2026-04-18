using System;
using System.Collections.Generic;

namespace POSSystem.Domain;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public decimal Salary { get; set; }
    public ShiftType ShiftType { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;

    public virtual Role Role { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<Order> AssignedOrders { get; set; } = new List<Order>();
}
