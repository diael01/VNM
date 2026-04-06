using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Simulators.Models;
using Microsoft.AspNetCore.Authorization;
using Infrastructure.Options;
using Repositories.Models;

namespace Simulators.Controllers;

[ApiController]
[Route("api/v1/consumption")]
[Authorize]
public class ConsumptionController : ControllerBase
{
    private readonly ConsumptionSimulatorOptions _options;
    private readonly Random _rand = new();

    public ConsumptionController(IOptions<ConsumptionSimulatorOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("data")]
    public ActionResult<ConsumptionReading> GetData()
    {
        var data = new ConsumptionReading
        {
            Power = new decimal(33333.3333), // _rand.Next(_options.MinConsumption, _options.MaxConsumption + 1),
            Timestamp = DateTime.UtcNow,
            AddressId = 2, // _rand.Next(1, 2)  //consumers are addresses 1 and 2
            Source = "simulator"
        };

        return Ok(data);
    }
}
