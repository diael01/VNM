using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Simulators.Models;
using Microsoft.AspNetCore.Authorization;
using Infrastructure.Options;
using Repositories.Models;

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
    public ActionResult<InverterReading> GetData()
    {

        var data = new InverterReading
        {
            Power = new decimal(55555.5555),//decimal)_options.MinPower + (decimal)_rand.NextDouble() * ((decimal)_options.MaxPower - (decimal)_options.MinPower);,
            Voltage = (decimal)_options.MinVoltage + (decimal)_rand.NextDouble() * ((decimal)_options.MaxVoltage - (decimal)_options.MinVoltage),
            Current = (decimal)_options.MinCurrent + (decimal)_rand.NextDouble() * ((decimal)_options.MaxCurrent - (decimal)_options.MinCurrent),
            Timestamp = DateTime.UtcNow,
            InverterInfoId = 1, //producer is only address 1 for now, so inverterInfoId is 1 as well. If we want to have multiple producers, we can make this random as well, like _rand.Next(_options.MinInverterId, _options.MaxInverterId + 1);        
            AddressId = 1, //same as above, we only have one producer address for now, so addressId is 1 as well. If we want to have multiple producers, we can make this random as well, like _rand.Next(_options.MinAddressId, _options.MaxAddressId + 1);
            Source = "simulator"
        };

        return Ok(data);
    }
}

