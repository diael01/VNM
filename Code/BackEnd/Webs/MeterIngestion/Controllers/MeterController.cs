using Microsoft.AspNetCore.Mvc;
using MassTransit;
using EventBusCore.Events;
using Microsoft.AspNetCore.Authorization;

namespace MeterIngestion.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class MeterController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MeterController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost("ingest")]
    // [Authorize(Roles = "admin")]
    public async Task<IActionResult> IngestData([FromBody] MeterIngestRequest request)
    {
        var evt = new MeterDataIngestedEvent
        {
            MeterId = request.MeterId,
            Value = request.Value,
        };

        await _publishEndpoint.Publish(evt);

        return Ok(new { Status = "Data ingested" });
    }
}

