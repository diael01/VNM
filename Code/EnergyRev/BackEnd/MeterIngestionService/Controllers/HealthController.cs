using Microsoft.AspNetCore.Mvc;


namespace MeterIngestionService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "Healthy", service = "meter-ingestion" });
    }
}
