using System;

namespace Repositories.Models;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    string? UpdatedBy { get; set; }
}

public abstract class AuditableEntity : IAuditableEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}