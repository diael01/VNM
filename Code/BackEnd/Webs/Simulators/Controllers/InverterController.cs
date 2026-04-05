using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Simulators.Models;
using Microsoft.AspNetCore.Authorization;
using Infrastructure.Options;

namespace Simulators.Controllers;

[ApiController]
[Route("api/v1/inverter")]
[Authorize]
//[Authorize(Roles = "admin")] //in this case bob cant query coz he is a contributor
public class InverterController : ControllerBase
{
    private readonly InverterSimulatorOptions _options;
    private readonly Random _rand = new();

    public InverterController(IOptions<InverterSimulatorOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("data")]
    public ActionResult<InverterData> GetData()
    {
        // Generate random decimal for Power
        decimal power = new decimal(55555.5555);//decimal)_options.MinPower + (decimal)_rand.NextDouble() * ((decimal)_options.MaxPower - (decimal)_options.MinPower);
        decimal voltage = (decimal)_options.MinVoltage + (decimal)_rand.NextDouble() * ((decimal)_options.MaxVoltage - (decimal)_options.MinVoltage);
        decimal current = (decimal)_options.MinCurrent + (decimal)_rand.NextDouble() * ((decimal)_options.MaxCurrent - (decimal)_options.MinCurrent);
        int inverterInfoId = 1; //producer is only address 1 for now, so inverterInfoId is 1 as well. If we want to have multiple producers, we can make this random as well, like _rand.Next(_options.MinInverterId, _options.MaxInverterId + 1);        
        var data = new InverterData(
            Power: power,
            Voltage: voltage,
            Current: current,
            Timestamp: DateTime.UtcNow,
            InverterInfoId: inverterInfoId
        );

        return Ok(data);
    }
}

