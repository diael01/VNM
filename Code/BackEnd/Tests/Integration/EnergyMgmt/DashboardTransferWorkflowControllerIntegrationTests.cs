using Infrastructure.DTOs;
using System.Net.Http.Json;

namespace EnergyManagementWeb.IntegrationTests;

public class DashboardTransferWorkflowControllerIntegrationTests : IClassFixture<DashboardBffCustomWebApplicationFactory>
{
    private readonly DashboardBffCustomWebApplicationFactory _factory;

    public DashboardTransferWorkflowControllerIntegrationTests(DashboardBffCustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Execute_Endpoint_Forwards_Note_And_Returns_Updated_Workflow()
    {
        var client = _factory.CreateClient();
        const string note = "Execute from BFF integration test";

        var response = await client.PostAsJsonAsync("api/v1/dashboard/transferWorkflows/123/execute", new { note });
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<TransferWorkflowDto>();
        Assert.NotNull(dto);
        Assert.Equal(123, dto!.Id);
        Assert.Equal(2, dto.Status);

        Assert.Equal(123, _factory.RedirectService.LastExecuteId);
        Assert.Equal(note, _factory.RedirectService.LastExecuteNote);
    }

    [Fact]
    public async Task Settle_Endpoint_Forwards_Note_And_Returns_Settled_Workflow()
    {
        var client = _factory.CreateClient();
        const string note = "Settle from BFF integration test";

        var response = await client.PostAsJsonAsync("api/v1/dashboard/transferWorkflows/123/settle", new { note });
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<TransferWorkflowDto>();
        Assert.NotNull(dto);
        Assert.Equal(123, dto!.Id);
        Assert.Equal(3, dto.Status);

        Assert.Equal(123, _factory.RedirectService.LastSettleId);
        Assert.Equal(note, _factory.RedirectService.LastSettleNote);
    }
}
