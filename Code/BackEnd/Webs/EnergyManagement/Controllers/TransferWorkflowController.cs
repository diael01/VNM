using Microsoft.AspNetCore.Mvc;
using Services.Transfers;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/TransferWorkflow")]
[Route("api/v1/transfer-workflows")]
public class TransferWorkflowController : ControllerBase
{
    private readonly ITransitionWorkflowService _transferWorkflowService;

    public TransferWorkflowController(ITransitionWorkflowService transferWorkflowService)
    {
        _transferWorkflowService = transferWorkflowService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var workflows = await _transferWorkflowService.GetAllAsync(ct);
        return Ok(workflows);
    }

    [HttpGet("history")]
    [HttpGet("~/api/v1/transfers/history")]
    public async Task<IActionResult> GetAllHistory(CancellationToken ct)
    {
        var history = await _transferWorkflowService.GetAllHistoryAsync(ct);
        return Ok(history);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var workflow = await _transferWorkflowService.GetByIdAsync(id, ct);
        if (workflow == null) return NotFound();
        return Ok(workflow);
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id, CancellationToken ct)
    {
        var history = await _transferWorkflowService.GetHistoryAsync(id, ct);
        return Ok(history);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] WorkflowActionRequest? request, CancellationToken ct)
    {
        try
        {
            var updated = await _transferWorkflowService.ApproveAsync(id, request?.Note, ct);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] WorkflowActionRequest? request, CancellationToken ct)
    {
        try
        {
            var updated = await _transferWorkflowService.RejectAsync(id, request?.Note, ct);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/execute")]
    public async Task<IActionResult> Execute(int id, [FromBody] WorkflowActionRequest? request, CancellationToken ct)
    {
        try
        {
            var updated = await _transferWorkflowService.ExecuteAsync(id, request?.Note, ct);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/settle")]
    public async Task<IActionResult> Settle(int id, [FromBody] WorkflowActionRequest? request, CancellationToken ct)
    {
        try
        {
            var updated = await _transferWorkflowService.SettleAsync(id, request?.Note, ct);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public sealed class WorkflowActionRequest
    {
        public string? Note { get; set; }
    }
}
