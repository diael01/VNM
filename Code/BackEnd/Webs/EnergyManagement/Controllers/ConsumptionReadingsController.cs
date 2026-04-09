using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Repositories.CRUD.Repositories;

namespace EnergyManagement.Controllers;

[ApiController]
 [Route("api/v1/[controller]")]
public class ConsumptionReadingsController : ControllerBase
{
    private readonly IConsumptionReadingRepository _consumptionReadingRepository;

    public ConsumptionReadingsController(IConsumptionReadingRepository consumptionReadingRepository)
    {
        _consumptionReadingRepository = consumptionReadingRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var readings = await _consumptionReadingRepository.GetAllAsync();
        return Ok(readings);
    }

    [HttpGet("latest/{count}")]
    public async Task<IActionResult> GetLatest(int count)
    {
        var readings = await _consumptionReadingRepository.GetLatestReadingsAsync(count);
        return Ok(readings);
    }
}