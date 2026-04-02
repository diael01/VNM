using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Infrastructure.DTOs;
using AutoMapper;
using Services.Inverter;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class InverterInfoController : ControllerBase
{
    private readonly IInverterInfoService _inverterInfoService;
    private readonly IMapper _mapper;

    public InverterInfoController(IInverterInfoService inverterInfoService, IMapper mapper)
    {
        _inverterInfoService = inverterInfoService;
        _mapper = mapper;
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
    public async Task<IActionResult> Create([FromBody] InverterInfoDto dto)
    {
        var created = await _inverterInfoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] InverterInfoDto dto)
    {
        if (id != dto.Id) return BadRequest();
        var updated = await _inverterInfoService.UpdateAsync(id, dto);
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