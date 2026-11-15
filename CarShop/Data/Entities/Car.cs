using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class Car
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public string Brand { get; set; } = null!;

    public string Color { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int ProductionYear { get; set; }

    public int Mileage { get; set; }

    public int Displacement { get; set; }

    public string FuelType { get; set; } = null!;

    public string Transmission { get; set; } = null!;

    public int Seats { get; set; }

    public int SellingPrice { get; set; }

    public int CostPrice { get; set; }

    public string Status { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<CarImage> CarImages { get; set; } = new List<CarImage>();

    public virtual ICollection<CarPost> CarPosts { get; set; } = new List<CarPost>();

    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<SalesRecord> SalesRecords { get; set; } = new List<SalesRecord>();

    public virtual ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
}
