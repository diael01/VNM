using Microsoft.AspNetCore.Mvc;
using EventBusClient;
using System.Threading.Tasks;

namespace MeterIngestionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeterController : ControllerBase
    {
        private readonly EventBusClientService _eventBus = new EventBusClientService();

        [HttpPost("ingest")]
        public async Task<IActionResult> IngestData([FromBody] object meterData)
        {
            await _eventBus.PublishAsync("MeterEvents", meterData);
            return Ok(new { Status = "Data ingested" });
        }
    }
}
