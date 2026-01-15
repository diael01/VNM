using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace EventBusMock.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventBusController : ControllerBase
    {
        private readonly Counter<long> _counter;

        public EventBusController(Meter meter)
        {
            _counter = meter.CreateCounter<long>("test.requests.eventbus");
        }

        [HttpPost("publish")]
        public IActionResult Publish([FromBody] object message)
        {
            Console.WriteLine($"[EventBusMock] Received message: {message}");
            return Ok(new { Status = "Message received" });
        }
    }
}
