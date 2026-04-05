using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Simulators.Models;
using Microsoft.AspNetCore.Authorization;
using Infrastructure.Options;

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
    public ActionResult<ConsumerReadingData> GetData()
    {
	    var data = new ConsumerReadingData(
            Power: new decimal(11111.1111),//_rand.Next(_options.MinConsumption, _options.MaxConsumption + 1),
            Timestamp: DateTime.UtcNow,
            AddressId: 1//_rand.Next(_options.MinInverterId, _options.MaxInverterId + 1)
        );

        return Ok(data);
    }
}
