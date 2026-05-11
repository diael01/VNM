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

    [HttpPost("settle/{sourceAddressId}/{destinationAddressId}/{day}")]
    public async Task<IActionResult> Settle(int sourceAddressId, int destinationAddressId, DateOnly day)
        => Ok(await _service.ProcessSettlementAsync(sourceAddressId, destinationAddressId, day));

    [HttpGet("{sourceAddressId}/{destinationAddressId}/{day}")]
    public async Task<IActionResult> Get(int sourceAddressId, int destinationAddressId, DateOnly day)
    {
        var result = await _db.ProviderSettlements
            .FirstOrDefaultAsync(x => x.SourceAddressId == sourceAddressId && x.DestinationAddressId == destinationAddressId && DateOnly.FromDateTime(x.CreatedAtUtc) == day);

        return result == null ? NotFound() : Ok(result);
    }
}