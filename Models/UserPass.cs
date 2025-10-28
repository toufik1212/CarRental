using System;
using System.Collections.Generic;

namespace CarRental.Models;

public partial class UserPass
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }
}
