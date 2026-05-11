using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.DTOs;
using Infrastructure.Utils;
using Moq;
using Services.Redirect;
using Xunit;

namespace Tests.Redirect;

public class DashboardRedirectServiceTests
{
    [Fact]
    public void CreateAuthorizedMeterClient_SetsBearerTokenOnNamedClient()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient("meter-api")).Returns(client);

        var result = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(factory.Object, "abc123");

        Assert.Same(client, result);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "abc123").ToString(), result.DefaultRequestHeaders.Authorization?.ToString());
        factory.Verify(x => x.CreateClient("meter-api"), Times.Once);
    }

    [Fact]
    public async Task TransferRuleService_GetRulesAsync_ReturnsEmptyList_WhenApiReturnsNullBody()
    {
        var service = CreateTransferRuleService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        });

        var rules = await service.GetTransferRulesAsync("token");

        Assert.Empty(rules);
    }

    [Fact]
    public async Task TransferRuleService_GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var service = CreateTransferRuleService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var rule = await service.GetTransferRuleByIdAsync("token", 42);

        Assert.Null(rule);
    }

    [Fact]
    public async Task TransferRuleService_CreateUpdateDelete_HandleSuccessAndErrors()
    {
        var requests = new List<HttpRequestMessage>();
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            JsonResponse(HttpStatusCode.Created, new TransferRuleDto { Id = 99, SourceTransferPolicyId = 7, DestinationAddressId = 8 }),
            JsonResponse(HttpStatusCode.OK, new TransferRuleDto { Id = 11, SourceTransferPolicyId = 7, DestinationAddressId = 9 }),
            new HttpResponseMessage(HttpStatusCode.NoContent),
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
        });

        var service = CreateTransferRuleService(_ =>
        {
            var response = responses.Dequeue();
            response.Headers.TryAddWithoutValidation("X-Test", "1");
            return response;
        }, requests);

        var createDto = new TransferRuleDto { Id = 5, SourceTransferPolicyId = 7, DestinationAddressId = 8 };
        var created = await service.CreateTransferRuleAsync("token", createDto);
        var updated = await service.UpdateTransferRuleAsync("token", 11, new TransferRuleDto { Id = 22, SourceTransferPolicyId = 7, DestinationAddressId = 9 });
        var deleted = await service.DeleteTransferRuleAsync("token", 77);

        Assert.Equal(99, created.Id);
        Assert.Equal(11, updated.Id);
        Assert.True(deleted);
        Assert.Equal(0, createDto.Id);
        Assert.Equal(11, requests[1].RequestUri!.ToString().Contains("/api/v1/TransferRule/11") ? 11 : 0);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteTransferRuleAsync("token", 88));
        Assert.Contains("status code 500", ex.Message);
    }

    [Fact]
    public async Task TransferWorkflowService_CoversSuccessNullAndFailurePaths()
    {
        var requests = new List<HttpRequestMessage>();
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            JsonResponse(HttpStatusCode.OK, new List<TransferWorkflowDto> { new() { Id = 1, Status = 0 } }),
            JsonResponse(HttpStatusCode.OK, new List<TransferWorkflowStatusHistoryDto> { new() { Id = 2 } }),
            new HttpResponseMessage(HttpStatusCode.NotFound),
            JsonResponse(HttpStatusCode.OK, new TransferWorkflowDto { Id = 3, Status = 1 }),
            JsonResponse(HttpStatusCode.OK, new TransferWorkflowDto { Id = 4, Status = 4 }),
            JsonResponse(HttpStatusCode.OK, new TransferWorkflowDto { Id = 5, Status = 2 }),
            JsonResponse(HttpStatusCode.OK, new TransferWorkflowDto { Id = 6, Status = 3 }),
            new HttpResponseMessage(HttpStatusCode.BadGateway)
        });

        var service = CreateTransferWorkflowService(_ => responses.Dequeue(), requests);

        var workflows = await service.GetTransferWorkflowsAsync("token");
        var history = await service.GetTransferWorkflowHistoryAsync("token");
        var missing = await service.GetTransferWorkflowByIdAsync("token", 99);
        var approved = await service.ApproveTransferWorkflowAsync("token", 1, "ok");
        var rejected = await service.RejectTransferWorkflowAsync("token", 1, "nope");
        var executed = await service.ExecuteTransferWorkflowAsync("token", 1, "run");
        var settled = await service.SettleTransferWorkflowAsync("token", 1, "done");

        Assert.Single(workflows);
        Assert.Single(history);
        Assert.Null(missing);
        Assert.Equal(1, approved.Status);
        Assert.Equal(4, rejected.Status);
        Assert.Equal(2, executed.Status);
        Assert.Equal(3, settled.Status);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetTransferWorkflowsAsync("token"));
        Assert.Contains("status code 502", ex.Message);
    }

    [Fact]
    public async Task SourceTransferPolicyService_CoversSuccessNullNotFoundAndFailure()
    {
        var requests = new List<HttpRequestMessage>();
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            JsonResponse(HttpStatusCode.OK, new List<SourceTransferPolicyDto> { new() { Id = 10, SourceAddressId = 2 } }),
            JsonResponse(HttpStatusCode.OK, new SourceTransferPolicyDto { Id = 11, SourceAddressId = 3 }),
            new HttpResponseMessage(HttpStatusCode.Created) { Content = JsonContent.Create(new SourceTransferPolicyDto { Id = 12, SourceAddressId = 4 }) },
            JsonResponse(HttpStatusCode.OK, new SourceTransferPolicyDto { Id = 13, SourceAddressId = 5 }),
            new HttpResponseMessage(HttpStatusCode.NotFound),
            JsonResponse(HttpStatusCode.OK, new List<TransferRuleDto> { new() { Id = 1, DestinationAddressId = 8 } }),
            JsonResponse(HttpStatusCode.OK, new List<SourceTransferScheduleDto> { new() { Id = 2, SourceTransferPolicyId = 3 } }),
            JsonResponse(HttpStatusCode.OK, new SourceTransferScheduleDto { Id = 14, SourceTransferPolicyId = 3 }),
            JsonResponse(HttpStatusCode.OK, new SourceTransferScheduleDto { Id = 15, SourceTransferPolicyId = 3 }),
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        var service = CreateSourcePolicyService(_ => responses.Dequeue(), requests);

        var policies = await service.GetPoliciesAsync("token");
        var byId = await service.GetPolicyByIdAsync("token", 11);
        var created = await service.CreatePolicyAsync("token", new SourceTransferPolicyDto { Id = 99, SourceAddressId = 4 });
        var updated = await service.UpdatePolicyAsync("token", 13, new SourceTransferPolicyDto { Id = 88, SourceAddressId = 5 });
        var missing = await service.GetPolicyByIdAsync("token", 404);
        var rules = await service.GetRulesAsync("token", 3);
        var schedules = await service.GetSchedulesAsync("token", 3);
        var scheduleCreated = await service.CreateScheduleAsync("token", 3, new SourceTransferScheduleDto { Id = 77, SourceTransferPolicyId = 0 });
        var scheduleUpdated = await service.UpdateScheduleAsync("token", 3, 15, new SourceTransferScheduleDto { Id = 78, SourceTransferPolicyId = 0 });
        var scheduleMissing = await service.DeleteScheduleAsync("token", 3, 404);
        var deleted = await service.DeletePolicyAsync("token", 55);

        Assert.Single(policies);
        Assert.NotNull(byId);
        Assert.Equal(12, created.Id);
        Assert.Equal(13, updated.Id);
        Assert.Null(missing);
        Assert.Single(rules);
        Assert.Single(schedules);
        Assert.Equal(3, scheduleCreated.SourceTransferPolicyId);
        Assert.Equal(3, scheduleUpdated.SourceTransferPolicyId);
        Assert.False(scheduleMissing);
        Assert.False(deleted);
        Assert.Equal(12, created.Id);
        Assert.Equal(13, updated.Id);
        Assert.Equal(3, scheduleCreated.SourceTransferPolicyId);

        var failingService = CreateSourcePolicyService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("boom")
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failingService.UpdatePolicyAsync("token", 99, new SourceTransferPolicyDto { SourceAddressId = 1 }));
        Assert.Contains("EnergyManagement API returned 500", ex.Message);
        Assert.Contains("boom", ex.Message);
    }

    private static IDashboardTransferRuleRedirectService CreateTransferRuleService(Func<HttpRequestMessage, HttpResponseMessage> responder, List<HttpRequestMessage>? requests = null)
        => new DashboardTransferRuleRedirectService(new FakeHttpClientFactory(new FakeHttpMessageHandler(responder, requests)));

    private static IDashboardTransferWorkflowRedirectService CreateTransferWorkflowService(Func<HttpRequestMessage, HttpResponseMessage> responder, List<HttpRequestMessage>? requests = null)
        => new DashboardTransferWorkflowRedirectService(new FakeHttpClientFactory(new FakeHttpMessageHandler(responder, requests)));

    private static IDashboardSourceTransferPolicyRedirectService CreateSourcePolicyService(Func<HttpRequestMessage, HttpResponseMessage> responder, List<HttpRequestMessage>? requests = null)
        => new DashboardSourceTransferPolicyRedirectService(new FakeHttpClientFactory(new FakeHttpMessageHandler(responder, requests)));

    private static HttpResponseMessage JsonResponse<T>(HttpStatusCode statusCode, T value)
        => new(statusCode) { Content = JsonContent.Create(value) };

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://example.test/")
            };
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        private readonly List<HttpRequestMessage>? _requests;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder, List<HttpRequestMessage>? requests = null)
        {
            _responder = responder;
            _requests = requests;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests?.Add(request);
            return Task.FromResult(_responder(request));
        }
    }
}
