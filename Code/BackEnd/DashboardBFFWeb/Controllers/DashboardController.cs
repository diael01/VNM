using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EventBusCore.Events;

namespace DashboardBFFWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public DashboardController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var evt = new DashboardStatusEvent
            {
                Message = "Service is running"
            };

            await _publishEndpoint.Publish(evt);

            return Ok(new { Service = "DashboardBFFWeb", Status = "Running" });
        }
    }
}

