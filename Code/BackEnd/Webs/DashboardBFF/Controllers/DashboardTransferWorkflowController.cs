using Infrastructure.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Redirect;

namespace DashboardBFF.Controllers;

[ApiController]
[Route("api/v1/dashboard/transferWorkflows")]
public class DashboardTransferWorkflowController : ControllerBase
{
    private readonly IDashboardTransferWorkflowRedirectService _dashboardService;

    public sealed class WorkflowActionRequest
    {
        public string? Note { get; set; }
    }

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

    [HttpGet("history")]
    [HttpGet("~/api/v1/dashboard/transfers/history")]
    [HttpGet("~/api/v1/dashboard/transferHistory")]
    [Authorize]
    public async Task<IActionResult> GetTransferWorkflowHistory(CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var history = await _dashboardService.GetTransferWorkflowHistoryAsync(accessToken, cancellationToken);
        return Ok(history);
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

    [HttpPost("{id}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveTransferWorkflow(int id, [FromBody] WorkflowActionRequest? request, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var updated = await _dashboardService.ApproveTransferWorkflowAsync(accessToken, id, request?.Note, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectTransferWorkflow(int id, [FromBody] WorkflowActionRequest? request, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var updated = await _dashboardService.RejectTransferWorkflowAsync(accessToken, id, request?.Note, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id}/execute")]
    [Authorize]
    public async Task<IActionResult> ExecuteTransferWorkflow(int id, [FromBody] WorkflowActionRequest? request, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var updated = await _dashboardService.ExecuteTransferWorkflowAsync(accessToken, id, request?.Note, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id}/settle")]
    [Authorize]
    public async Task<IActionResult> SettleTransferWorkflow(int id, [FromBody] WorkflowActionRequest? request, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var updated = await _dashboardService.SettleTransferWorkflowAsync(accessToken, id, request?.Note, cancellationToken);
        return Ok(updated);
    }
}
