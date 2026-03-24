using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.DTOs;
using Infrastructure.Validation;
using Services.Redirect;
using Microsoft.AspNetCore.Authentication;
using Infrastructure.Utils;

namespace DashboardBFF.Controllers
{
    [ApiController]
    [Route("api/v1/dashboard/addressInfo")]
    public class DashboardAddressController : ControllerBase
    {
        private readonly IDashboardAddressRedirectService _dashboardService;
        public DashboardAddressController(IDashboardAddressRedirectService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
        {
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var addresses = await _dashboardService.GetAddressesAsync(accessToken, cancellationToken);
            return Ok(addresses);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetAddressById(int id, CancellationToken cancellationToken)
        {
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var address = await _dashboardService.GetAddressByIdAsync(accessToken, id, cancellationToken);
            if (address == null) return NotFound();
            return Ok(address);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateAddress([FromBody] AddressDto addressDto, CancellationToken cancellationToken)
        {
            var validator = new AddressDtoValidator();
            var validationResult = validator.Validate(addressDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var created = await _dashboardService.CreateAddressAsync(accessToken, addressDto, cancellationToken);
            return CreatedAtAction(nameof(GetAddressById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] AddressDto addressDto, CancellationToken cancellationToken)
        {
            var validator = new AddressDtoValidator();
            var validationResult = validator.Validate(addressDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var updated = await _dashboardService.UpdateAddressAsync(accessToken, id, addressDto, cancellationToken);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAddress(int id, CancellationToken cancellationToken)
        {
            var accessToken = await HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
            var deleted = await _dashboardService.DeleteAddressAsync(accessToken, id, cancellationToken);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
