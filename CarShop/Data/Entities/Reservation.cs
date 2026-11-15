using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class Reservation
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public int UserId { get; set; }

    public DateTime ReservationDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Car Car { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
