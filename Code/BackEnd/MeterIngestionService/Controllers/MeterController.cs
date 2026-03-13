using Microsoft.AspNetCore.Mvc;
using MassTransit;
using EventBusCore.Events;

namespace MeterIngestionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeterController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MeterController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost("ingest")]
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
