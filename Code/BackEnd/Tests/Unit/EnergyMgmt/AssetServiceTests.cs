using AutoMapper;
using Infrastructure.DTOs;
using Moq;
using Repositories.CRUD.Repositories;
using Repositories.Models;
using Services.Inverter;
using Xunit;

namespace Tests.Inverter;

public class AssetServiceTests
{
    [Fact]
    public async Task AddressService_CoversCrudAndMissingBranch()
    {
        var repository = new Mock<IAddressRepository>();
        var mapper = new Mock<IMapper>();
        var service = new AddressService(repository.Object, mapper.Object);

        var dto = new AddressDto
        {
            Country = "RO",
            County = "CJ",
            City = "Cluj",
            Street = "Main",
            StreetNumber = "1",
            PostalCode = "400000",
            InverterId = 7
        };

        var mapped = new Address
        {
            Id = 0,
            Country = dto.Country,
            County = dto.County,
            City = dto.City,
            Street = dto.Street,
            StreetNumber = dto.StreetNumber,
            PostalCode = dto.PostalCode
        };

        repository.Setup(x => x.AddAsync(mapped, It.IsAny<CancellationToken>())).ReturnsAsync(new Address { Id = 10, Country = dto.Country, County = dto.County, City = dto.City, Street = dto.Street, StreetNumber = dto.StreetNumber, PostalCode = dto.PostalCode });
        repository.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new Address { Id = 10, Country = dto.Country, County = dto.County, City = dto.City, Street = dto.Street, StreetNumber = dto.StreetNumber, PostalCode = dto.PostalCode });
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { new Address { Id = 10, Country = dto.Country, County = dto.County, City = dto.City, Street = dto.Street, StreetNumber = dto.StreetNumber, PostalCode = dto.PostalCode } });
        repository.Setup(x => x.DeleteAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address address, CancellationToken _) => address);
        mapper.Setup(x => x.Map<Address>(dto)).Returns(mapped);
        mapper.Setup(x => x.Map(dto, It.IsAny<Address>())).Callback<AddressDto, Address>((source, target) =>
        {
            target.Country = source.Country;
            target.County = source.County;
            target.City = source.City;
            target.Street = source.Street;
            target.StreetNumber = source.StreetNumber;
            target.PostalCode = source.PostalCode;
        });

        var created = await service.CreateAsync(dto);
        var fetched = await service.GetByIdAsync(10);
        var all = (await service.GetAllAsync()).ToList();
        var updated = await service.UpdateAsync(10, dto);
        var deleted = await service.DeleteAsync(10);

        Assert.Equal(10, created.Id);
        Assert.NotNull(fetched);
        Assert.Single(all);
        Assert.Equal(10, updated.Id);
        Assert.True(deleted);

        repository.Setup(x => x.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Address?)null);
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(404, dto));
        Assert.Contains("Address 404", ex.Message);
    }

    [Fact]
    public async Task InverterInfoService_CoversCrudAndMissingBranch()
    {
        var repository = new Mock<IInverterInfoRepository>();
        var mapper = new Mock<IMapper>();
        var service = new InverterInfoService(repository.Object, mapper.Object);

        var dto = new InverterInfoDto
        {
            Id = 5,
            Model = "M",
            Manufacturer = "Maker",
            SerialNumber = "SN",
            AddressId = 10
        };

        var entity = new InverterInfo
        {
            Id = 0,
            Model = dto.Model!,
            Manufacturer = dto.Manufacturer!,
            SerialNumber = dto.SerialNumber!,
            AddressId = dto.AddressId
        };

        repository.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(new InverterInfo
        {
            Id = 11,
            Model = dto.Model!,
            Manufacturer = dto.Manufacturer!,
            SerialNumber = dto.SerialNumber!,
            AddressId = dto.AddressId
        });
        repository.Setup(x => x.GetByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(new InverterInfo
        {
            Id = 11,
            Model = dto.Model!,
            Manufacturer = dto.Manufacturer!,
            SerialNumber = dto.SerialNumber!,
            AddressId = dto.AddressId
        });
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new InverterInfo
            {
                Id = 11,
                Model = dto.Model!,
                Manufacturer = dto.Manufacturer!,
                SerialNumber = dto.SerialNumber!,
                AddressId = dto.AddressId
            }
        });
        repository.Setup(x => x.UpdateAsync(It.IsAny<InverterInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InverterInfo info, CancellationToken _) => info);
        repository.Setup(x => x.DeleteAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mapper.Setup(x => x.Map<InverterInfo>(dto)).Returns(entity);
        mapper.Setup(x => x.Map<InverterInfoDto>(It.IsAny<InverterInfo>()))
            .Returns((InverterInfo info) => new InverterInfoDto
            {
                Id = info.Id,
                Model = info.Model,
                Manufacturer = info.Manufacturer,
                SerialNumber = info.SerialNumber,
                AddressId = info.AddressId
            });
        mapper.Setup(x => x.Map(dto, It.IsAny<InverterInfo>())).Callback<InverterInfoDto, InverterInfo>((source, target) =>
        {
            target.Model = source.Model!;
            target.Manufacturer = source.Manufacturer!;
            target.SerialNumber = source.SerialNumber!;
            target.AddressId = source.AddressId;
        });

        var created = await service.CreateAsync(dto);
        var fetched = await service.GetByIdAsync(11);
        var all = (await service.GetAllAsync()).ToList();
        var updated = await service.UpdateAsync(11, dto);
        var deleted = await service.DeleteAsync(11);

        Assert.Equal(11, created.Id);
        Assert.NotNull(fetched);
        Assert.Single(all);
        Assert.Equal(11, updated.Id);
        Assert.True(deleted);

        repository.Setup(x => x.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((InverterInfo?)null);
        var missing = await service.GetByIdAsync(404);
        Assert.Null(missing);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(404, dto));
        Assert.Contains("InverterInfo 404", ex.Message);
    }
}
