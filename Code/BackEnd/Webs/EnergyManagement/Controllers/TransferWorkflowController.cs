using Infrastructure.DTOs;
using Infrastructure.Validation;
using Microsoft.AspNetCore.Mvc;
using Services.Transfers;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/TransferWorkflow")]
[Route("api/v1/transfer-workflows")]
public class TransferWorkflowController : ControllerBase
{
    private readonly ITransferWorkflowCrudService _transferWorkflowService;

    public TransferWorkflowController(ITransferWorkflowCrudService transferWorkflowService)
    {
        _transferWorkflowService = transferWorkflowService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var workflows = await _transferWorkflowService.GetAllAsync();
        return Ok(workflows);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var workflow = await _transferWorkflowService.GetByIdAsync(id);
        if (workflow == null) return NotFound();
        return Ok(workflow);
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var history = await _transferWorkflowService.GetHistoryAsync(id);
        return Ok(history);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransferWorkflowDto transferWorkflowDto)
    {
        var validator = new TransferWorkflowDtoValidator();
        var validationResult = validator.Validate(transferWorkflowDto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var created = await _transferWorkflowService.CreateAsync(transferWorkflowDto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TransferWorkflowDto transferWorkflowDto)
    {
        var validator = new TransferWorkflowDtoValidator();
        var validationResult = validator.Validate(transferWorkflowDto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            var updated = await _transferWorkflowService.UpdateAsync(id, transferWorkflowDto);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _transferWorkflowService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] WorkflowActionRequest? request)
    {
        try
        {
            var updated = await _transferWorkflowService.ApproveAsync(id, request?.Note);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] WorkflowActionRequest? request)
    {
        try
        {
            var updated = await _transferWorkflowService.RejectAsync(id, request?.Note);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/execute")]
    public async Task<IActionResult> Execute(int id, [FromBody] WorkflowActionRequest? request)
    {
        try
        {
            var updated = await _transferWorkflowService.ExecuteAsync(id, request?.Note);
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
