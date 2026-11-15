using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class Order
{
    public int Id { get; set; }

    public string OrderNo { get; set; } = null!;

    public int CarId { get; set; }

    public int UserId { get; set; }

    public int DepositAmount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public string? TradeNo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual Car Car { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
