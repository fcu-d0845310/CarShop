using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class SalesRecord
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public int UserId { get; set; }

    public int FinalPrice { get; set; }

    public DateTime SaleDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Car Car { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
