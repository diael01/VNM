using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Simulators.Configuration;
using Simulators.Models;
using Microsoft.AspNetCore.Authorization;

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
        var data = new InverterData(
            Power: _rand.Next(_options.MinPower, _options.MaxPower + 1),
            Voltage: _rand.Next(_options.MinVoltage, _options.MaxVoltage + 1),
            Current: _rand.Next(_options.MinCurrent, _options.MaxCurrent + 1),
            Timestamp: DateTime.UtcNow,
            InverterInfoId: 1//_rand.Next(_options.MinInverterId, _options.MaxInverterId + 1)
        );

        return Ok(data);
    }
}

