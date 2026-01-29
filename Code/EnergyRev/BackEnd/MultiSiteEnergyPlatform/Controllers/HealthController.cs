using Microsoft.AspNetCore.Mvc;


namespace MultiSiteEnergyPlatform.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "Healthy", service = "multi-site-energy-platform" });
    }
}
