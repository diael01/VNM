using Microsoft.AspNetCore.Mvc;

namespace EventBusMock.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "Healthy", service = "eventbus-mock" });
    }
}