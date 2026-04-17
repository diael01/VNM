using Infrastructure.DTOs;
using Infrastructure.Utils;
using Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Redirect;

namespace DashboardBFF.Controllers;

[ApiController]
[Route("api/v1/dashboard/sourcePolicies")]
public class DashboardSourceTransferPolicyController : ControllerBase
{
    private readonly IDashboardSourceTransferPolicyRedirectService _service;

    public DashboardSourceTransferPolicyController(IDashboardSourceTransferPolicyRedirectService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetPolicies(CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        return Ok(await _service.GetPoliciesAsync(accessToken, cancellationToken));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetPolicyById(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var policy = await _service.GetPolicyByIdAsync(accessToken, id, cancellationToken);
        if (policy is null) return NotFound();
        return Ok(policy);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePolicy([FromBody] SourceTransferPolicyDto dto, CancellationToken cancellationToken)
    {
        var v = new SourceTransferPolicyDtoValidator();
        var r = v.Validate(dto);
        if (!r.IsValid) return BadRequest(r.Errors);

        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var created = await _service.CreatePolicyAsync(accessToken, dto, cancellationToken);
        return CreatedAtAction(nameof(GetPolicyById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdatePolicy(int id, [FromBody] SourceTransferPolicyDto dto, CancellationToken cancellationToken)
    {
        var v = new SourceTransferPolicyDtoValidator();
        var r = v.Validate(dto);
        if (!r.IsValid) return BadRequest(r.Errors);

        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        return Ok(await _service.UpdatePolicyAsync(accessToken, id, dto, cancellationToken));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePolicy(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        if (!await _service.DeletePolicyAsync(accessToken, id, cancellationToken)) return NotFound();
        return NoContent();
    }

    // --- Nested rules ---

    [HttpGet("{id}/rules")]
    [Authorize]
    public async Task<IActionResult> GetRules(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        return Ok(await _service.GetRulesAsync(accessToken, id, cancellationToken));
    }

    // --- Nested schedules ---

    [HttpGet("{id}/schedules")]
    [Authorize]
    public async Task<IActionResult> GetSchedules(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        return Ok(await _service.GetSchedulesAsync(accessToken, id, cancellationToken));
    }

    [HttpPost("{id}/schedules")]
    [Authorize]
    public async Task<IActionResult> CreateSchedule(int id, [FromBody] SourceTransferScheduleDto dto, CancellationToken cancellationToken)
    {
        var v = new SourceTransferScheduleDtoValidator();
        var r = v.Validate(dto);
        if (!r.IsValid) return BadRequest(r.Errors);

        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var created = await _service.CreateScheduleAsync(accessToken, id, dto, cancellationToken);
        return Created($"api/v1/dashboard/sourcePolicies/{id}/schedules/{created.Id}", created);
    }

    [HttpPut("{id}/schedules/{scheduleId}")]
    [Authorize]
    public async Task<IActionResult> UpdateSchedule(int id, int scheduleId, [FromBody] SourceTransferScheduleDto dto, CancellationToken cancellationToken)
    {
        var v = new SourceTransferScheduleDtoValidator();
        var r = v.Validate(dto);
        if (!r.IsValid) return BadRequest(r.Errors);

        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        return Ok(await _service.UpdateScheduleAsync(accessToken, id, scheduleId, dto, cancellationToken));
    }

    [HttpDelete("{id}/schedules/{scheduleId}")]
    [Authorize]
    public async Task<IActionResult> DeleteSchedule(int id, int scheduleId, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        if (!await _service.DeleteScheduleAsync(accessToken, id, scheduleId, cancellationToken)) return NotFound();
        return NoContent();
    }
}
