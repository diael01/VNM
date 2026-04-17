using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class AspNetRole : AuditableEntity
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public virtual ICollection<AspNetRoleClaim> AspNetRoleClaims { get; set; } = new List<AspNetRoleClaim>();

    public virtual ICollection<AspNetUser> Users { get; set; } = new List<AspNetUser>();
}
