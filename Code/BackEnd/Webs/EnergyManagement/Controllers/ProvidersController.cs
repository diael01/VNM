using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/providers")]
public class ProvidersController : ControllerBase
{
    private readonly VnmDbContext _db;
    private readonly EnergyManagement.Services.Providers.IProviderSettlementService _service;

    public ProvidersController(VnmDbContext db, EnergyManagement.Services.Providers.IProviderSettlementService service)
    {
        _db = db;
        _service = service;
    }

    [HttpPost("settle/{addressId}/{day}")]
    public async Task<IActionResult> Settle(int addressId, DateOnly day)
        => Ok(await _service.ProcessSettlementAsync(addressId, day));

    [HttpGet("{addressId}/{day}")]
    public async Task<IActionResult> Get(int addressId, DateOnly day)
    {
        var result = await _db.ProviderSettlements
            .FirstOrDefaultAsync(x => x.AddressId == addressId && DateOnly.FromDateTime(x.Day) == day);

        return result == null ? NotFound() : Ok(result);
    }
}