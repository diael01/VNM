
using Microsoft.AspNetCore.Mvc;
using Infrastructure.DTOs;
using Services.Inverter;
using AutoMapper;

namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]

public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;
    private readonly IMapper _mapper;

    public AddressController(IAddressService addressService, IMapper mapper)
    {
        _addressService = addressService;
        _mapper = mapper;
    }


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var addresses = await _addressService.GetAllAsync();
        var dtos = _mapper.Map<List<AddressDto>>(addresses);
        return Ok(dtos);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var address = await _addressService.GetByIdAsync(id);
        if (address == null) return NotFound();
        var dto = _mapper.Map<AddressDto>(address);
        return Ok(dto);
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddressDto addressDto)
    {
        var created = await _addressService.CreateAsync(addressDto);
        var dto = _mapper.Map<AddressDto>(created);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AddressDto addressDto)
    {
        var updated = await _addressService.UpdateAsync(id, addressDto);
        var dto = _mapper.Map<AddressDto>(updated);
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _addressService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}