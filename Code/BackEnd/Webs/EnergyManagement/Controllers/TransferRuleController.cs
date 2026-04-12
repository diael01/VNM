using Microsoft.AspNetCore.Mvc;
using Infrastructure.DTOs;
using Infrastructure.Validation;
using Services.Transfers;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TransferRuleController : ControllerBase
{
    private readonly ITransferRuleService _transferRuleService;

    public TransferRuleController(ITransferRuleService transferRuleService)
    {
        _transferRuleService = transferRuleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rules = await _transferRuleService.GetAllAsync();
        return Ok(rules);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rule = await _transferRuleService.GetByIdAsync(id);
        if (rule == null) return NotFound();
        return Ok(rule);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransferRuleDto transferRuleDto)
    {
        var validator = new TransferRuleDtoValidator();
        var validationResult = validator.Validate(transferRuleDto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var created = await _transferRuleService.CreateAsync(transferRuleDto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TransferRuleDto transferRuleDto)
    {
        var validator = new TransferRuleDtoValidator();
        var validationResult = validator.Validate(transferRuleDto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var updated = await _transferRuleService.UpdateAsync(id, transferRuleDto);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _transferRuleService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
