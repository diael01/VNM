using Infrastructure.Utils;
using Infrastructure.DTOs;
using Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Redirect;

namespace DashboardBFF.Controllers;

[ApiController]
[Route("api/v1/dashboard/transferRules")]
public class DashboardTransferRuleController : ControllerBase
{
    private readonly IDashboardTransferRuleRedirectService _dashboardService;

    public DashboardTransferRuleController(IDashboardTransferRuleRedirectService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTransferRules(CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var rules = await _dashboardService.GetTransferRulesAsync(accessToken, cancellationToken);
        return Ok(rules);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetTransferRuleById(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var rule = await _dashboardService.GetTransferRuleByIdAsync(accessToken, id, cancellationToken);
        if (rule == null) return NotFound();
        return Ok(rule);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTransferRule([FromBody] TransferRuleDto rule, CancellationToken cancellationToken)
    {
        var validator = new TransferRuleDtoValidator();
        var validationResult = validator.Validate(rule);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var created = await _dashboardService.CreateTransferRuleAsync(accessToken, rule, cancellationToken);
        return CreatedAtAction(nameof(GetTransferRuleById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateTransferRule(int id, [FromBody] TransferRuleDto rule, CancellationToken cancellationToken)
    {
        var validator = new TransferRuleDtoValidator();
        var validationResult = validator.Validate(rule);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var updated = await _dashboardService.UpdateTransferRuleAsync(accessToken, id, rule, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteTransferRule(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var deleted = await _dashboardService.DeleteTransferRuleAsync(accessToken, id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
