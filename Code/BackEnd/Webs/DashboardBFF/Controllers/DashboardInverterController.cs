using Repositories.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Redirect;
using Infrastructure.DTOs;
using Microsoft.AspNetCore.Authentication;
using Infrastructure.Utils;

namespace DashboardBFF.Controllers
{
    [ApiController]
    [Route("api/v1/dashboard/inverterInfo")]
    public class DashboardInverterController : ControllerBase
    {
        private readonly IDashboardInverterRedirectService _dashboardService;
        public DashboardInverterController(IDashboardInverterRedirectService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("inverterreadings")]
        [Authorize]
        public async Task<IActionResult> GetInverterReadings(CancellationToken cancellationToken)
        {
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var readings = await _dashboardService.GetInverterReadingsAsync(accessToken, cancellationToken) ?? new List<InverterReading>();
            return Ok(readings);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetInverters(CancellationToken cancellationToken)
        {
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var inverters = await _dashboardService.GetAllInverterInfoAsync(accessToken, cancellationToken);
            return Ok(inverters);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetInverterById(int id, CancellationToken cancellationToken)
        {
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var inverter = await _dashboardService.GetInverterInfoByIdAsync(accessToken, id, cancellationToken);
            if (inverter == null) return NotFound();
            return Ok(inverter);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateInverter([FromBody] InverterInfoDto info, CancellationToken cancellationToken)
        {
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var created = await _dashboardService.CreateInverterInfoAsync(accessToken, info, cancellationToken);
            return CreatedAtAction(nameof(GetInverterById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateInverter(int id, [FromBody] InverterInfoDto info, CancellationToken cancellationToken)
        {
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var updated = await _dashboardService.UpdateInverterInfoAsync(accessToken, id, info, cancellationToken);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteInverter(int id, CancellationToken cancellationToken)
        {
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var deleted = await _dashboardService.DeleteInverterInfoAsync(accessToken, id, cancellationToken);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
