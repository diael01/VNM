using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Repositories.CRUD.Repositories;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InverterReadingsController : ControllerBase
{
    private readonly IInverterReadingRepository _inverterReadingRepository;

    public InverterReadingsController(IInverterReadingRepository inverterReadingRepository)
    {
        _inverterReadingRepository = inverterReadingRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var readings = await _inverterReadingRepository.GetAllAsync();
        return Ok(readings);
    }

    [HttpGet("latest/{count}")]
    public async Task<IActionResult> GetLatest(int count)
    {
        var readings = await _inverterReadingRepository.GetLatestReadingsAsync(count);
        return Ok(readings);
    }

}

