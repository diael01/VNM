using Microsoft.AspNetCore.Mvc;
using EventBusClient;
using System.Threading.Tasks;

namespace MultiSiteEnergyPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlatformController : ControllerBase
    {
        private readonly EventBusClientService _eventBus = new EventBusClientService();

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            await _eventBus.PublishAsync("MeterEvents", "Sample message from MultiSiteEnergyPlatform");
            return Ok(new { Service = "MultiSiteEnergyPlatform", Status = "Running" });
        }
    }
}
