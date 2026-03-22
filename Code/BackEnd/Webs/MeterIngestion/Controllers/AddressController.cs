using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Services.Inverter;

namespace MeterIngestion.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;

    public AddressController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var addresses = await _addressService.GetAllAsync();
        return Ok(addresses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var address = await _addressService.GetByIdAsync(id);
        if (address == null) return NotFound();
        return Ok(address);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Address address)
    {
        var created = await _addressService.CreateAsync(address);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Address address)
    {
        if (id != address.Id) return BadRequest();
        var updated = await _addressService.UpdateAsync(address);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _addressService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}