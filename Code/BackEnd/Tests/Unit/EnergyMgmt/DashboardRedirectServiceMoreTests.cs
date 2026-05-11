using AutoMapper;
using Infrastructure.DTOs;
using Infrastructure.Utils;
using Moq;
using Repositories.Models;
using Services.Redirect;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tests.Redirect;

public class DashboardRedirectServiceMoreTests
{
    [Fact]
    public async Task InverterService_CoversReadAndCrudBranches()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            JsonResponse(HttpStatusCode.OK, new List<InverterReading> { new() { Power = 1m, Voltage = 2m, Current = 3m } }),
            JsonResponse(HttpStatusCode.OK, new List<InverterInfoDto> { new() { Id = 10, AddressId = 2 } }),
            JsonResponse(HttpStatusCode.OK, new InverterInfoDto { Id = 11, AddressId = 3 }),
            new HttpResponseMessage(HttpStatusCode.NotFound),
            JsonResponse(HttpStatusCode.Created, new InverterInfoDto { Id = 12, AddressId = 4 }),
            JsonResponse(HttpStatusCode.OK, new InverterInfoDto { Id = 13, AddressId = 5 }),
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new HttpResponseMessage(HttpStatusCode.NoContent),
            new HttpResponseMessage(HttpStatusCode.BadRequest)
        });

        var service = new DashboardInverterRedirectService(new FakeHttpClientFactory(_ => responses.Dequeue()));

        var readings = await service.GetInverterReadingsAsync("token");
        var infos = await service.GetAllInverterInfoAsync("token");
        var info = await service.GetInverterInfoByIdAsync("token", 11);
        var missing = await service.GetInverterInfoByIdAsync("token", 404);
        var created = await service.CreateInverterInfoAsync("token", new InverterInfoDto { Id = 99, AddressId = 4 });
        var updated = await service.UpdateInverterInfoAsync("token", 13, new InverterInfoDto { Id = 98, AddressId = 5 });
        var deleteMissing = await service.DeleteInverterInfoAsync("token", 404);
        var deleted = await service.DeleteInverterInfoAsync("token", 13);

        Assert.Single(readings);
        Assert.Single(infos);
        Assert.NotNull(info);
        Assert.Null(missing);
        Assert.Equal(12, created.Id);
        Assert.Equal(13, updated.Id);
        Assert.False(deleteMissing);
        Assert.True(deleted);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetInverterReadingsAsync("token"));
        Assert.Contains("status code 400", ex.Message);
    }

    [Fact]
    public async Task DailyBalanceService_CoversSuccessNullAndFailure()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            JsonResponse(HttpStatusCode.OK, new List<DailyEnergyBalance> { new() { Id = 1, AddressId = 10 } }),
            JsonResponse<List<DailyEnergyBalance>?>(HttpStatusCode.OK, null),
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
        });

        var service = new DashboardDailyBalanceRedirectService(new FakeHttpClientFactory(_ => responses.Dequeue()));

        var balances = await service.GetDailyBalanceAsync("token");
        var empty = await service.GetDailyBalanceAsync("token");

        Assert.Single(balances);
        Assert.Empty(empty);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDailyBalanceAsync("token"));
        Assert.Contains("status code 500", ex.Message);
    }

    [Fact]
    public async Task ConsumptionService_CoversBranches()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            JsonResponse(HttpStatusCode.OK, new List<ConsumptionReading> { new() { Id = 1, AddressId = 2, Source = "s" } }),
            JsonResponse(HttpStatusCode.OK, new ConsumptionReading { Id = 2, AddressId = 3, Source = "x" }),
            new HttpResponseMessage(HttpStatusCode.NotFound),
            JsonResponse(HttpStatusCode.Created, new ConsumptionReading { Id = 3, AddressId = 4, Source = "c" }),
            JsonResponse(HttpStatusCode.OK, new ConsumptionReading { Id = 4, AddressId = 5, Source = "u" }),
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
        });

        var service = new DashboardConsumptionRedirectService(new FakeHttpClientFactory(_ => responses.Dequeue()));

        var all = await service.GetAllConsumptionReadingsAsync("token");
        var byId = await service.GetConsumptionReadingByIdAsync("token", 2);
        var missing = await service.GetConsumptionReadingByIdAsync("token", 404);
        var created = await service.CreateConsumptionReadingAsync("token", new ConsumptionReading { Id = 99, AddressId = 4, Source = "c" });
        var updated = await service.UpdateConsumptionReadingAsync("token", 4, new ConsumptionReading { Id = 98, AddressId = 5, Source = "u" });
        var deleteMissing = await service.DeleteConsumptionReadingAsync("token", 404);

        Assert.Single(all);
        Assert.NotNull(byId);
        Assert.Null(missing);
        Assert.Equal(3, created.Id);
        Assert.Equal(4, updated.Id);
        Assert.False(deleteMissing);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAllConsumptionReadingsAsync("token"));
        Assert.Contains("status code 500", ex.Message);
    }

    [Fact]
    public async Task AddressService_CoversBranches()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            JsonResponse(HttpStatusCode.OK, new List<Address> { new() { Id = 1, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" } }),
            JsonResponse(HttpStatusCode.OK, new Address { Id = 2, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" }),
            new HttpResponseMessage(HttpStatusCode.NotFound),
            JsonResponse(HttpStatusCode.Created, new Address { Id = 3, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" }),
            JsonResponse(HttpStatusCode.OK, new Address { Id = 4, Country = "A", County = "B", City = "C", Street = "S", StreetNumber = "1", PostalCode = "000" }),
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new HttpResponseMessage(HttpStatusCode.BadGateway)
        });

        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<Address>(It.IsAny<AddressDto>()))
            .Returns((AddressDto dto) => new Address
            {
                Id = dto.Id,
                Country = dto.Country,
                County = dto.County,
                City = dto.City,
                Street = dto.Street,
                StreetNumber = dto.StreetNumber,
                PostalCode = dto.PostalCode
            });

        var service = new DashboardAddressRedirectService(new FakeHttpClientFactory(_ => responses.Dequeue()), mapper.Object);

        var all = await service.GetAddressesAsync("token");
        var byId = await service.GetAddressByIdAsync("token", 2);
        var missing = await service.GetAddressByIdAsync("token", 404);
        var created = await service.CreateAddressAsync("token", new AddressDto
        {
            Id = 99,
            Country = "A",
            County = "B",
            City = "C",
            Street = "S",
            StreetNumber = "1",
            PostalCode = "000",
            InverterId = 5
        });
        var updated = await service.UpdateAddressAsync("token", 4, new AddressDto
        {
            Id = 98,
            Country = "A",
            County = "B",
            City = "C",
            Street = "S",
            StreetNumber = "1",
            PostalCode = "000",
            InverterId = 5
        });
        var deleteMissing = await service.DeleteAddressAsync("token", 404);

        Assert.Single(all);
        Assert.NotNull(byId);
        Assert.Null(missing);
        Assert.Equal(3, created.Id);
        Assert.Equal(4, updated.Id);
        Assert.False(deleteMissing);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAddressesAsync("token"));
        Assert.Contains("status code 502", ex.Message);
    }

    private static HttpResponseMessage JsonResponse<T>(HttpStatusCode statusCode, T value)
        => new(statusCode) { Content = JsonContent.Create(value) };

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _client = new HttpClient(new FakeHttpMessageHandler(responder))
            {
                BaseAddress = new Uri("https://example.test/")
            };
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}
