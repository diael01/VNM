using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Services.Identity;

namespace DashboardBFF.Controllers;

[ApiController]
[Route("api/v1/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IAspNetIdentityService _identityService;

    public PermissionsController(IAspNetIdentityService identityService)
    {
        _identityService = identityService;
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

    [HttpPost("roles/{roleId}/users/{userId}")]
    public async Task<IActionResult> AssignRole(string userId, string roleId, CancellationToken cancellationToken)
    {
        var result = await _identityService.AssignRoleToUserAsync(userId, roleId, cancellationToken);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("roles/{roleId}/users/{userId}")]
    public async Task<IActionResult> RemoveRole(string userId, string roleId, CancellationToken cancellationToken)
    {
        var result = await _identityService.RemoveRoleFromUserAsync(userId, roleId, cancellationToken);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpGet("users/{userId}/claims")]
    public async Task<IActionResult> GetUserClaims(string userId, CancellationToken cancellationToken)
    {
        var claims = await _identityService.GetEffectiveUserClaimsAsync(userId, cancellationToken);
        return Ok(claims.Select(c => new { c.ClaimType, c.ClaimValue }));
    }
}
