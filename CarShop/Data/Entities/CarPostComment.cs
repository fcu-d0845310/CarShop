using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class CarPostComment
{
    public int Id { get; set; }

    public int PostId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = null!;

    public int? ParentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CarPostComment> InverseParent { get; set; } = new List<CarPostComment>();

    public virtual CarPostComment? Parent { get; set; }

    public virtual CarPost Post { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
