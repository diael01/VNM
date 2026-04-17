using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class AspNetUserClaim : AuditableEntity
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string? ClaimType { get; set; }

    public string? ClaimValue { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}
