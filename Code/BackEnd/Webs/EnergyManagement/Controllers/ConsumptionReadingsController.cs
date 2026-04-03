using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Services.Meter;

namespace EnergyManagement.Controllers;

[ApiController]
 [Route("api/v1/[controller]")]
public class ConsumptionReadingsController : ControllerBase
{
    private readonly IConsumptionReadingService _consumptionReadingService;

    public ConsumptionReadingsController(IConsumptionReadingService consumptionReadingService)
    {
        _consumptionReadingService = consumptionReadingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var readings = await _consumptionReadingService.GetAllAsync();
        return Ok(readings);
    }

    [HttpGet("latest/{count}")]
    public async Task<IActionResult> GetLatest(int count)
    {
        var readings = await _consumptionReadingService.GetLatestAsync(count);
        return Ok(readings);
    }
}