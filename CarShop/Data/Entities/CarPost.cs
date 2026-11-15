using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class CarPost
{
    public int Id { get; set; }

    public int? CarId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Category { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Car? Car { get; set; }

    public virtual ICollection<CarPostComment> CarPostComments { get; set; } = new List<CarPostComment>();

    public virtual User User { get; set; } = null!;
}
