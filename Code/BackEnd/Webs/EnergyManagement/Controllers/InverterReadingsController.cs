using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Services.Inverter;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InverterReadingsController : ControllerBase
{
    private readonly IInverterReadingService _inverterReadingService;

    public InverterReadingsController(IInverterReadingService inverterReadingService)
    {
        _inverterReadingService = inverterReadingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var readings = await _inverterReadingService.GetAllAsync();
        return Ok(readings);
    }

    [HttpGet("latest/{count}")]
    public async Task<IActionResult> GetLatest(int count)
    {
        var readings = await _inverterReadingService.GetLatestAsync(count);
        return Ok(readings);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InverterReading reading)
    {
        var created = await _inverterReadingService.CreateAsync(reading);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }
}

