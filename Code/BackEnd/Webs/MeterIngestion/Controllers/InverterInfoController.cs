using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Services.Inverter;

namespace MeterIngestion.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InverterInfoController : ControllerBase
{
    private readonly IInverterInfoService _inverterInfoService;

    public InverterInfoController(IInverterInfoService inverterInfoService)
    {
        _inverterInfoService = inverterInfoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var infos = await _inverterInfoService.GetAllAsync();
        return Ok(infos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var info = await _inverterInfoService.GetByIdAsync(id);
        if (info == null) return NotFound();
        return Ok(info);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InverterInfo info)
    {
        var created = await _inverterInfoService.CreateAsync(info);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] InverterInfo info)
    {
        if (id != info.Id) return BadRequest();
        var updated = await _inverterInfoService.UpdateAsync(info);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _inverterInfoService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}