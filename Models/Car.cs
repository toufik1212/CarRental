using System;
using System.Collections.Generic;

namespace CarRental.Models;

public partial class Car
{
    public int Id { get; set; }

    public string? Model { get; set; }

    public decimal? PricePerDay { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdateAt { get; set; }
    public string? PlatNo { get; set; }

    public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();
}
