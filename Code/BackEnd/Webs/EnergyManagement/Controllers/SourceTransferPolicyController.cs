using Infrastructure.DTOs;
using Infrastructure.Validation;
using Microsoft.AspNetCore.Mvc;
using Services.Transfers;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SourceTransferPolicyController : ControllerBase
{
    private readonly ISourceTransferPolicyService _policyService;
    private readonly ISourceTransferScheduleService _scheduleService;

    public SourceTransferPolicyController(
        ISourceTransferPolicyService policyService,
        ISourceTransferScheduleService scheduleService)
    {
        _policyService = policyService;
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var policies = await _policyService.GetAllAsync(cancellationToken);
        return Ok(policies);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var policy = await _policyService.GetByIdAsync(id, cancellationToken);
        if (policy is null) return NotFound();
        return Ok(policy);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SourceTransferPolicyDto dto, CancellationToken cancellationToken)
    {
        var validator = new SourceTransferPolicyDtoValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        var created = await _policyService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SourceTransferPolicyDto dto, CancellationToken cancellationToken)
    {
        var validator = new SourceTransferPolicyDtoValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        var updated = await _policyService.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _policyService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }

    // --- Nested: destination rules for this policy ---

    [HttpGet("{id}/rules")]
    public async Task<IActionResult> GetRules(int id, CancellationToken cancellationToken)
    {
        var rules = await _policyService.GetRulesAsync(id, cancellationToken);
        return Ok(rules);
    }

    // --- Nested: schedules for this policy ---

    [HttpGet("{id}/schedules")]
    public async Task<IActionResult> GetSchedules(int id, CancellationToken cancellationToken)
    {
        var schedules = await _policyService.GetSchedulesAsync(id, cancellationToken);
        return Ok(schedules);
    }

    [HttpPost("{id}/schedules")]
    public async Task<IActionResult> AddSchedule(int id, [FromBody] SourceTransferScheduleDto dto, CancellationToken cancellationToken)
    {
        dto.SourceTransferPolicyId = id;
        var validator = new SourceTransferScheduleDtoValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        var created = await _scheduleService.CreateAsync(dto, cancellationToken);
        return Created($"api/v1/SourceTransferPolicy/{id}/schedules/{created.Id}", created);
    }

    [HttpPut("{id}/schedules/{scheduleId}")]
    public async Task<IActionResult> UpdateSchedule(int id, int scheduleId, [FromBody] SourceTransferScheduleDto dto, CancellationToken cancellationToken)
    {
        dto.SourceTransferPolicyId = id;
        var validator = new SourceTransferScheduleDtoValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        var updated = await _scheduleService.UpdateAsync(scheduleId, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id}/schedules/{scheduleId}")]
    public async Task<IActionResult> DeleteSchedule(int id, int scheduleId, CancellationToken cancellationToken)
    {
        var deleted = await _scheduleService.DeleteAsync(scheduleId, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
