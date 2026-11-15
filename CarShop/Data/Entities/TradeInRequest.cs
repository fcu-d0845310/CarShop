using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class TradeInRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int ProductionYear { get; set; }

    public int Mileage { get; set; }

    public int ExpectedPrice { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<TradeInImage> TradeInImages { get; set; } = new List<TradeInImage>();

    public virtual User User { get; set; } = null!;
}
