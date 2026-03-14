using Microsoft.AspNetCore.Mvc;
using Services.Identity;
using Repositories.Models;

namespace MeterIngestionWeb.Controllers;

[ApiController]
[Route("api/permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IAspNetIdentityService _identityService;

    public PermissionsController(IAspNetIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedDefaults(CancellationToken cancellationToken)
    {
        await _identityService.SeedDefaultPermissionsAsync(cancellationToken);
        return Ok(new { success = true, message = "Default roles, users and claims seeded." });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _identityService.GetAllRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _identityService.GetAllUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("user/{userId}/claims")]
    public async Task<IActionResult> GetUserClaims(string userId, CancellationToken cancellationToken)
    {
        var claims = await _identityService.GetEffectiveUserClaimsAsync(userId, cancellationToken);
        return Ok(claims.Select(c => new { c.ClaimType, c.ClaimValue }));
    }

    [HttpGet("user/byname/{username}/claims")]
    public async Task<IActionResult> GetUserClaimsByName(string username, CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByUserNameAsync(username, cancellationToken);
        if (user == null) return NotFound();

        var claims = await _identityService.GetEffectiveUserClaimsAsync(user.Id, cancellationToken);
        return Ok(claims.Select(c => new { c.ClaimType, c.ClaimValue }));
    }
}
