using Infrastructure.DTOs;
using Infrastructure.Validation;
using Microsoft.AspNetCore.Mvc;
using Services.Transfers;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
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

        var updated = await _transferWorkflowService.UpdateAsync(id, transferWorkflowDto);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _transferWorkflowService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
