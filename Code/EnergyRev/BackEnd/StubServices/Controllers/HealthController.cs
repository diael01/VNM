using Microsoft.AspNetCore.Mvc;

namespace StubServices.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "Healthy", service = "stub-services" });
    }
}
