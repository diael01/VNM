using Infrastructure.DTOs;
using Microsoft.AspNetCore.Mvc;
using EnergyManagement.Services.Transfers.Execution;
using Services.Transfers;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/transfer-execution/workflows")]
public class TransferExecutionController : ControllerBase
{
    private readonly ITransferExecutionService _transferExecutionService;
    private readonly ITransitionWorkflowService _transferWorkflowService;

    public TransferExecutionController(
        ITransferExecutionService transferExecutionService,
        ITransitionWorkflowService transferWorkflowService)
    {
        _transferExecutionService = transferExecutionService;
        _transferWorkflowService = transferWorkflowService;
    }

    [HttpPost("{id}/execute")]
    public async Task<IActionResult> Execute(int id, [FromBody] WorkflowActionRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            var executedBy = User?.Identity?.Name;
            await _transferExecutionService.ExecuteAsync(id, executedBy, request?.Note, cancellationToken);

            var updated = await _transferWorkflowService.GetByIdAsync(id);
            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/settle")]
    public async Task<IActionResult> Settle(int id, [FromBody] WorkflowActionRequest? request)
    {
        try
        {
            var updated = await _transferWorkflowService.SettleAsync(id, request?.Note);
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
