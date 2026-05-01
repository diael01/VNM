using Infrastructure.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Repositories.Models;

namespace EnergyManagementWeb.IntegrationTests;

public class TransferExecutionControllerIntegrationTests : IntegrationTestBase, IClassFixture<TransferExecutionIntegrationFactory>
{
    public TransferExecutionControllerIntegrationTests(TransferExecutionIntegrationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Execute_Then_Settle_Persists_Notes_And_Status_Transitions()
    {
        var client = Factory.CreateClient();
        const string executeNote = "Execute from approved tab";
        const string settleNote = "Settle now";

        var workflowId = await SeedWorkflowAsync(status: 1);

        var executeResponse = await client.PostAsJsonAsync($"api/v1/transfer-execution/workflows/{workflowId}/execute", new { note = executeNote });
        executeResponse.EnsureSuccessStatusCode();

        var executedDto = await executeResponse.Content.ReadFromJsonAsync<TransferWorkflowDto>();
        Assert.NotNull(executedDto);
        Assert.Equal(2, executedDto!.Status);

        await using (var executeScope = Factory.Services.CreateAsyncScope())
        {
            var executeDbFactory = executeScope.ServiceProvider.GetRequiredService<IDbContextFactory<VnmDbContext>>();
            await using var executeDb = await executeDbFactory.CreateDbContextAsync();

            var ledger = await executeDb.TransferLedgerEntries
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.TransferWorkflowId == workflowId);

            Assert.NotNull(ledger);
            Assert.Equal(executeNote, ledger!.Notes);

            var executeHistory = await executeDb.TransferWorkflowStatusHistory
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.TransferWorkflowId == workflowId && x.ToStatus == 2);

            Assert.NotNull(executeHistory);
            Assert.Contains("UserNote=Execute from approved tab", executeHistory!.Note);
        }

        var settleResponse = await client.PostAsJsonAsync($"api/v1/transfer-execution/workflows/{workflowId}/settle", new { note = settleNote });
        settleResponse.EnsureSuccessStatusCode();

        var settledDto = await settleResponse.Content.ReadFromJsonAsync<TransferWorkflowDto>();
        Assert.NotNull(settledDto);
        Assert.Equal(3, settledDto!.Status);

        await using var settleScope = Factory.Services.CreateAsyncScope();
        var settleDbFactory = settleScope.ServiceProvider.GetRequiredService<IDbContextFactory<VnmDbContext>>();
        await using var settleDb = await settleDbFactory.CreateDbContextAsync();

        var workflow = await settleDb.TransferWorkflows.FirstOrDefaultAsync(x => x.Id == workflowId);
        Assert.NotNull(workflow);
        Assert.Equal(3, workflow!.Status);

        var settleHistory = await settleDb.TransferWorkflowStatusHistory
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(x => x.TransferWorkflowId == workflowId && x.ToStatus == 3);

        Assert.NotNull(settleHistory);
        Assert.Equal(settleNote, settleHistory!.Note);
    }

    private async Task<int> SeedWorkflowAsync(int status)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<VnmDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var source = new Address
        {
            Country = "RO",
            County = "B",
            City = "Bucharest",
            Street = "Seed Source",
            StreetNumber = "1",
            PostalCode = "000001"
        };

        var destination = new Address
        {
            Country = "RO",
            County = "B",
            City = "Bucharest",
            Street = "Seed Destination",
            StreetNumber = "2",
            PostalCode = "000002"
        };

        db.Addresses.Add(source);
        db.Addresses.Add(destination);
        await db.SaveChangesAsync();

        var workflow = new TransferWorkflow
        {
            EffectiveAtUtc = DateTime.UtcNow,
            BalanceDayUtc = DateTime.UtcNow.Date,
            SourceAddressId = source.Id,
            DestinationAddressId = destination.Id,
            SourceSurplusKwhAtWorkflow = 15,
            DestinationDeficitKwhAtWorkflow = 10,
            RemainingSourceSurplusKwhAfterWorkflow = 5,
            AmountKwh = 10,
            TriggerType = 0,
            Status = status,
            SettlementMode = 0,
            AppliedDistributionMode = 0,
            Notes = "seed"
        };

        db.TransferWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        return workflow.Id;
    }
}

public sealed class TransferExecutionIntegrationFactory : CustomWebApplicationFactory
{
    public TransferExecutionIntegrationFactory() : base()
    {
    }
}
