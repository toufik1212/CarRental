using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace CarRental.Models;

public partial class Rental
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public int UserId { get; set; }

    public DateTime? RentDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public DateTime? ActualReturnDate { get; set; }

    public int? TotalDays { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ValidateNever]
    public virtual Car Car { get; set; } = null!;

    [ValidateNever]
    public virtual UserPass? UserPass { get; set; }

}
