using System;
using System.Collections.Generic;

namespace CarShop.Data.Entities;

public partial class Branch
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
}
