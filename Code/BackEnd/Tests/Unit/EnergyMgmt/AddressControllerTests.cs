using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Services.Inverter;
// using Services.DTOs; (removed, not needed)
using EnergyManagement.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.DTOs;

namespace EnergyManagement.UnitTests.Controllers;

using AutoMapper;

public class AddressControllerTests
{
    private readonly Mock<IAddressService> _mockService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly AddressController _controller;

    public AddressControllerTests()
    {
        _mockService = new Mock<IAddressService>();
        _mockMapper = new Mock<IMapper>();
        _controller = new AddressController(_mockService.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithAddresses()
    {
        var addressesList = new List<Repositories.Models.Address> { new Repositories.Models.Address { Id = 1, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" } };
        _mockService.Setup(s => s.GetAllAsync(default)).ReturnsAsync(addressesList);
        var dtosList = new List<AddressDto> { new AddressDto { Id = 1, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" } };
        _mockMapper.Setup(m => m.Map<List<AddressDto>>(addressesList)).Returns(dtosList);
        var result = await _controller.GetAll();
        var ok = Assert.IsType<OkObjectResult>(result);
        var addresses = Assert.IsAssignableFrom<IEnumerable<AddressDto>>(ok.Value);
        Assert.Single(addresses);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var entity = new Repositories.Models.Address { Id = 1, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" };
        var dto = new AddressDto { Id = 1, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" };
        _mockService.Setup(s => s.GetByIdAsync(1, default)).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AddressDto>(entity)).Returns(dto);
        var result = await _controller.GetById(1);
        var ok = Assert.IsType<OkObjectResult>(result);
        var address = Assert.IsType<AddressDto>(ok.Value);
        Assert.Equal("A", address.Country);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.GetByIdAsync(2, default)).ReturnsAsync((Repositories.Models.Address?)null);
        var result = await _controller.GetById(2);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAt_WithAddress()
    {
        var dto = new AddressDto { Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" };
        var entity = new Repositories.Models.Address { Id = 3, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" };
        var createdDto = new AddressDto { Id = 3, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" };
        _mockService.Setup(s => s.CreateAsync(dto, default)).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AddressDto>(entity)).Returns(createdDto);
        var result = await _controller.Create(dto);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var value = Assert.IsType<AddressDto>(created.Value);
        Assert.Equal("A", value.Country);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenIdMatches()
    {
        var dto = new AddressDto { Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" };
        var entity = new Repositories.Models.Address { Id = 4, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" };
        var updatedDto = new AddressDto { Id = 4, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" };
        _mockService.Setup(s => s.UpdateAsync(4, dto, default)).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AddressDto>(entity)).Returns(updatedDto);
        var result = await _controller.Update(4, dto);
        var ok = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsType<AddressDto>(ok.Value);
        Assert.Equal("A", value.Country);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenIdMismatch()
    {
        var dto = new AddressDto { Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" };
        var result = await _controller.Update(6, dto);
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenDeleted()
    {
        _mockService.Setup(s => s.DeleteAsync(7, default)).ReturnsAsync(true);
        var result = await _controller.Delete(7);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenNotDeleted()
    {
        _mockService.Setup(s => s.DeleteAsync(8, default)).ReturnsAsync(false);
        var result = await _controller.Delete(8);
        Assert.IsType<NotFoundResult>(result);
    }
}
