using System.Threading;
using Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Repositories.Models;

[ApiController]
[Route("api/v1/consumption")]
[Authorize]
public class ConsumptionController : ControllerBase
{
    private readonly ConsumptionSimulatorOptions _options;
    private static int _counter = -1;

    public ConsumptionController(IOptions<ConsumptionSimulatorOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("data")]
    public ActionResult<ConsumptionReading> GetData()
    {
        var addressIds = new[] { 2, 3, 4 };
        var index = Interlocked.Increment(ref _counter) % addressIds.Length;

        var data = new ConsumptionReading
        {
            Power = 33333.3333m,
            Timestamp = DateTime.UtcNow,
            AddressId = addressIds[index],
            Source = "simulator"
        };

        return Ok(data);
    }
}