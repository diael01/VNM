using Infrastructure.DTOs;
using Infrastructure.Utils;
using Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Redirect;

namespace DashboardBFF.Controllers;

[ApiController]
[Route("api/v1/dashboard/transferWorkflows")]
public class DashboardTransferWorkflowController : ControllerBase
{
    private readonly IDashboardTransferWorkflowRedirectService _dashboardService;

    public DashboardTransferWorkflowController(IDashboardTransferWorkflowRedirectService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTransferWorkflows(CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var workflows = await _dashboardService.GetTransferWorkflowsAsync(accessToken, cancellationToken);
        return Ok(workflows);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetTransferWorkflowById(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var workflow = await _dashboardService.GetTransferWorkflowByIdAsync(accessToken, id, cancellationToken);
        if (workflow == null) return NotFound();
        return Ok(workflow);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTransferWorkflow([FromBody] TransferWorkflowDto workflow, CancellationToken cancellationToken)
    {
        var validator = new TransferWorkflowDtoValidator();
        var validationResult = validator.Validate(workflow);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var created = await _dashboardService.CreateTransferWorkflowAsync(accessToken, workflow, cancellationToken);
        return CreatedAtAction(nameof(GetTransferWorkflowById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateTransferWorkflow(int id, [FromBody] TransferWorkflowDto workflow, CancellationToken cancellationToken)
    {
        var validator = new TransferWorkflowDtoValidator();
        var validationResult = validator.Validate(workflow);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var updated = await _dashboardService.UpdateTransferWorkflowAsync(accessToken, id, workflow, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteTransferWorkflow(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var deleted = await _dashboardService.DeleteTransferWorkflowAsync(accessToken, id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
