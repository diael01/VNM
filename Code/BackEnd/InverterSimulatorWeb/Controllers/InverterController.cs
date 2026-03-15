using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using InverterSimulatorWeb.Configuration;
using InverterSimulatorWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace InverterSimulatorWeb.Controllers;

[ApiController]
[Route("api/inverter")]
[Authorize]
//[Authorize(Roles = "admin")] //in this case bob cant query coz he is a contributor
public class InverterController : ControllerBase
{
    private readonly InverterSimulatorWebOptions _options;
    private readonly Random _rand = new();

    public InverterController(IOptions<InverterSimulatorWebOptions> options)
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
            Timestamp: DateTime.UtcNow
        );

        return Ok(data);
    }
}

