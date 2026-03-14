using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class AspNetUser
{
    public string Id { get; set; } = null!;

    public string ExternalSubjectId { get; set; } = null!;

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public virtual ICollection<AspNetUserClaim> AspNetUserClaims { get; set; } = new List<AspNetUserClaim>();

    public virtual ICollection<AspNetRole> Roles { get; set; } = new List<AspNetRole>();
}
