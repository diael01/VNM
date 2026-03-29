using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Redirect;
using Repositories.Models;
using Microsoft.AspNetCore.Authentication;
using Infrastructure.Utils;

namespace DashboardBFF.Controllers;

[ApiController]
[Route("api/v1/dashboard/consumptionreadings")]
public class DashboardConsumptionController : ControllerBase
{
    private readonly IDashboardConsumptionRedirectService _dashboardService;
    public DashboardConsumptionController(IDashboardConsumptionRedirectService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var readings = await _dashboardService.GetAllConsumptionReadingsAsync(accessToken, cancellationToken);
        return Ok(readings);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var reading = await _dashboardService.GetConsumptionReadingByIdAsync(accessToken, id, cancellationToken);
        if (reading == null) return NotFound();
        return Ok(reading);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] ConsumptionReading reading, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var created = await _dashboardService.CreateConsumptionReadingAsync(accessToken, reading, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] ConsumptionReading reading, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var updated = await _dashboardService.UpdateConsumptionReadingAsync(accessToken, id, reading, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
        var deleted = await _dashboardService.DeleteConsumptionReadingAsync(accessToken, id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
