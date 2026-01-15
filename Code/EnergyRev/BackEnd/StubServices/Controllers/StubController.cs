using Microsoft.AspNetCore.Mvc;

namespace StubServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StubController : ControllerBase
    {
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { Service = "StubServices", Status = "Running" });
        }
    }
}
