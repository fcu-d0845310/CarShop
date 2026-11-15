using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class TradeInImage
{
    public int Id { get; set; }

    public int TradeInRequestId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual TradeInRequest TradeInRequest { get; set; } = null!;
}
