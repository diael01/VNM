using EnergyManagement.Services.ModeSwitching;
using Infrastructure.Enums;
using Infrastructure.Options;
using Microsoft.Extensions.Options;
using Repositories.Models;
using Xunit;

namespace Tests.ModeSwitching;

public class SettlementModeStrategyTests
{
    [Fact]
    public void EnergyStrategy_FillSettlement_SetsEnergyCreditFields()
    {
        var sut = new EnergySettlementModeStrategy();
        var settlement = new ProviderSettlement();
        var balance = new DailyEnergyBalance { SurplusKwh = 10m };

        sut.FillSettlement(settlement, balance, ratePerKwh: 0.25m, acceptanceRate: 0.8m);

        Assert.Equal(10m, settlement.SubmittedKwh);
        Assert.Equal(8m, settlement.SettledKwh);
        Assert.Equal(0.25m, settlement.RatePerKwh);
        Assert.Equal(0m, settlement.MonetaryCredit);
        Assert.Equal(8m, settlement.EnergyCreditKwh);
        Assert.Equal(ProviderSettlementMode.EnergyCredit, settlement.SettlementModeEnum);
    }

    [Fact]
    public void EnergyStrategy_ValidateRequest_ThrowsForInvalidAmount()
    {
        var sut = new EnergySettlementModeStrategy();
        var available = new AvailableTransferBalanceDto { AvailableKwh = 5m };

        var ex = Assert.Throws<InvalidOperationException>(() => sut.ValidateRequest(0m, available));

        Assert.Contains("Amount must be > 0", ex.Message);
    }

    [Fact]
    public void EnergyStrategy_ValidateRequest_ThrowsWhenInsufficientEnergy()
    {
        var sut = new EnergySettlementModeStrategy();
        var available = new AvailableTransferBalanceDto { AvailableKwh = 4m };

        var ex = Assert.Throws<InvalidOperationException>(() => sut.ValidateRequest(5m, available));

        Assert.Contains("Not enough energy balance", ex.Message);
    }

    [Fact]
    public void EnergyStrategy_ValidateRequest_AllowsWhenEnoughEnergy()
    {
        var sut = new EnergySettlementModeStrategy();
        var available = new AvailableTransferBalanceDto { AvailableKwh = 5m };

        sut.ValidateRequest(5m, available);
    }

    [Fact]
    public void EnergyStrategy_BuildImpact_ComputesCoveredAndRemaining()
    {
        var sut = new EnergySettlementModeStrategy();
        var transfer = new TransferExecutionRequest
        {
            DestinationAddressId = 77,
            BalanceDay = new DateOnly(2026, 5, 10),
            AmountKwh = 6m
        };
        var balance = new DailyEnergyBalance { DeficitKwh = 4m };

        var impact = sut.BuildImpact(transfer, balance, rate: 0.3m);

        Assert.Equal(77, impact.DestinationAddressId);
        Assert.Equal(new DateOnly(2026, 5, 10), impact.Day);
        Assert.Equal(4m, impact.OriginalDeficitKwh);
        Assert.Equal(4m, impact.CoveredByTransferKwh);
        Assert.Equal(0m, impact.RemainingDeficitKwh);
        Assert.Equal(1.2m, impact.OriginalCost);
        Assert.Equal(1.2m, impact.CoveredValue);
        Assert.Equal(0m, impact.RemainingCost);
    }

    [Fact]
    public void EnergyStrategy_BuildImpact_KeepsPositiveRemainingDeficit()
    {
        var sut = new EnergySettlementModeStrategy();
        var transfer = new TransferExecutionRequest
        {
            DestinationAddressId = 9,
            BalanceDay = new DateOnly(2026, 5, 10),
            AmountKwh = 2m
        };
        var balance = new DailyEnergyBalance { DeficitKwh = 5m };

        var impact = sut.BuildImpact(transfer, balance, rate: 0.4m);

        Assert.Equal(2m, impact.CoveredByTransferKwh);
        Assert.Equal(3m, impact.RemainingDeficitKwh);
        Assert.Equal(1.2m, impact.RemainingCost);
    }

    [Fact]
    public void MoneyStrategy_FillSettlement_SetsMonetaryFields()
    {
        var sut = new MoneySettlementModeStrategy();
        var settlement = new ProviderSettlement();
        var balance = new DailyEnergyBalance { SurplusKwh = 10m };

        sut.FillSettlement(settlement, balance, ratePerKwh: 0.5m, acceptanceRate: 0.6m);

        Assert.Equal(10m, settlement.SubmittedKwh);
        Assert.Equal(6m, settlement.SettledKwh);
        Assert.Equal(0.5m, settlement.RatePerKwh);
        Assert.Equal(3m, settlement.MonetaryCredit);
        Assert.Equal(0m, settlement.EnergyCreditKwh);
        Assert.Equal(ProviderSettlementMode.Monetary, settlement.SettlementModeEnum);
    }

    [Fact]
    public void MoneyStrategy_ValidateRequest_ThrowsForInsufficientMoney()
    {
        var sut = new MoneySettlementModeStrategy();
        var available = new AvailableTransferBalanceDto { AvailableMoney = 20m };

        var ex = Assert.Throws<InvalidOperationException>(() => sut.ValidateRequest(21m, available));

        Assert.Contains("Not enough money balance", ex.Message);
    }

    [Fact]
    public void MoneyStrategy_ValidateRequest_ThrowsForInvalidAmount()
    {
        var sut = new MoneySettlementModeStrategy();
        var available = new AvailableTransferBalanceDto { AvailableMoney = 20m };

        var ex = Assert.Throws<InvalidOperationException>(() => sut.ValidateRequest(0m, available));

        Assert.Contains("Amount must be > 0", ex.Message);
    }

    [Fact]
    public void MoneyStrategy_ValidateRequest_AllowsWhenEnoughMoney()
    {
        var sut = new MoneySettlementModeStrategy();
        var available = new AvailableTransferBalanceDto { AvailableMoney = 20m };

        sut.ValidateRequest(20m, available);
    }

    [Fact]
    public void MoneyStrategy_BuildImpact_ComputesMonetaryCoverage()
    {
        var sut = new MoneySettlementModeStrategy();
        var transfer = new TransferExecutionRequest
        {
            DestinationAddressId = 88,
            BalanceDay = new DateOnly(2026, 5, 10),
            AmountKwh = 9m
        };
        var balance = new DailyEnergyBalance { DeficitKwh = 10m };

        var impact = sut.BuildImpact(transfer, balance, rate: 3m);

        Assert.Equal(10m, impact.OriginalDeficitKwh);
        Assert.Equal(3m, impact.CoveredByTransferKwh);
        Assert.Equal(7m, impact.RemainingDeficitKwh);
        Assert.Equal(30m, impact.OriginalCost);
        Assert.Equal(9m, impact.CoveredValue);
        Assert.Equal(21m, impact.RemainingCost);
    }

    [Fact]
    public void MoneyStrategy_BuildImpact_ClampsRemainingCostToZero()
    {
        var sut = new MoneySettlementModeStrategy();
        var transfer = new TransferExecutionRequest
        {
            DestinationAddressId = 12,
            BalanceDay = new DateOnly(2026, 5, 10),
            AmountKwh = 40m
        };
        var balance = new DailyEnergyBalance { DeficitKwh = 10m };

        var impact = sut.BuildImpact(transfer, balance, rate: 3m);

        Assert.Equal(0m, impact.RemainingCost);
    }

    [Fact]
    public void Resolver_GetCurrentMode_ParsesConfiguredValue_CaseInsensitive()
    {
        var resolver = new SettlementModeResolver(
            Array.Empty<ISettlementModeStrategy>(),
            Options.Create(new SettlementModeOptions { CurrentMode = "energycredit" }));

        var mode = resolver.GetCurrentMode();

        Assert.Equal(ProviderSettlementMode.EnergyCredit, mode);
    }

    [Fact]
    public void Resolver_GetCurrentMode_FallsBackToMoney_ForInvalidValue()
    {
        var resolver = new SettlementModeResolver(
            Array.Empty<ISettlementModeStrategy>(),
            Options.Create(new SettlementModeOptions { CurrentMode = "unknown-mode" }));

        var mode = resolver.GetCurrentMode();

        Assert.Equal(ProviderSettlementMode.Monetary, mode);
    }

    [Fact]
    public void Resolver_Resolve_ReturnsMatchingStrategy()
    {
        var strategies = new ISettlementModeStrategy[]
        {
            new MoneySettlementModeStrategy(),
            new EnergySettlementModeStrategy()
        };

        var resolver = new SettlementModeResolver(
            strategies,
            Options.Create(new SettlementModeOptions { CurrentMode = "Money" }));

        var strategy = resolver.Resolve(ProviderSettlementMode.EnergyCredit);

        Assert.IsType<EnergySettlementModeStrategy>(strategy);
    }

    [Fact]
    public void Resolver_Resolve_ThrowsWhenStrategyMissing()
    {
        var resolver = new SettlementModeResolver(
            new ISettlementModeStrategy[] { new MoneySettlementModeStrategy() },
            Options.Create(new SettlementModeOptions { CurrentMode = "Money" }));

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(ProviderSettlementMode.EnergyCredit));
    }
}
