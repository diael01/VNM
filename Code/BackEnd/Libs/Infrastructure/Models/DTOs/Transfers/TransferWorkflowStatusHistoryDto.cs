namespace Infrastructure.DTOs;

public class TransferWorkflowStatusHistoryDto
{
    public int Id { get; set; }
    public int TransferWorkflowId { get; set; }
    public int? FromStatus { get; set; }
    public int ToStatus { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = null!;
}
