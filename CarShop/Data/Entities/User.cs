using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string? Password { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? IdentityNumber { get; set; }

    public string? IdentityNumberHash { get; set; }

    public string? Address { get; set; }

    public int RoleId { get; set; }

    public string AuthProvider { get; set; } = null!;

    public string? ProviderId { get; set; }

    public bool IsProfileCompleted { get; set; }

    public DateTime? PrivacyAgreedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<CarPostComment> CarPostComments { get; set; } = new List<CarPostComment>();

    public virtual ICollection<CarPost> CarPosts { get; set; } = new List<CarPost>();

    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SalesRecord> SalesRecords { get; set; } = new List<SalesRecord>();

    public virtual ICollection<TradeInRequest> TradeInRequests { get; set; } = new List<TradeInRequest>();

    public virtual ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
}
