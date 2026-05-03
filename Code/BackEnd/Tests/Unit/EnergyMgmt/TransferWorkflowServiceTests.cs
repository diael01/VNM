using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnergyManagement.Services.Transfers;
using Infrastructure.Enums;
using Infrastructure.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Repositories.Models;
using Xunit;

namespace Tests.Transfers;

public class TransferWorkflowServiceTests
{
    [Fact]
    public async Task RunAutomaticAllocationAsync_Fair_SplitsEqually()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Db;

        SeedAddresses(db, 1, 2, 3);

        db.DailyEnergyBalances.AddRange(
            new DailyEnergyBalance
            {
                AddressId = 1,
                InverterInfoId = 1,
                Day = UtcDay("2026-04-05"),
                SurplusKwh = 10m,
                DeficitKwh = 0m,
                ProducedKwh = 10m,
                ConsumedKwh = 0m,
                NetKwh = 10m,
                NetPerAddressKwh = 10m,
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Computed"
            },
            new DailyEnergyBalance
            {
                AddressId = 2,
                InverterInfoId = 2,
                Day = UtcDay("2026-04-05"),
                SurplusKwh = 0m,
                DeficitKwh = 10m,
                ProducedKwh = 0m,
                ConsumedKwh = 10m,
                NetKwh = -10m,
                NetPerAddressKwh = -10m,
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Computed"
            },
            new DailyEnergyBalance
            {
                AddressId = 3,
                InverterInfoId = 3,
                Day = UtcDay("2026-04-05"),
                SurplusKwh = 0m,
                DeficitKwh = 10m,
                ProducedKwh = 0m,
                ConsumedKwh = 10m,
                NetKwh = -10m,
                NetPerAddressKwh = -10m,
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Computed"
            });

        var fairPolicy = new SourceTransferPolicy
        {
            Id = 1,
            SourceAddressId = 1,
            DistributionModeEnum = TransferDistributionMode.Fair,
            IsEnabled = true,
        };

        db.SourceTransferPolicies.Add(fairPolicy);
        db.DestinationTransferRules.AddRange(
            new DestinationTransferRule
            {
                SourceTransferPolicyId = fairPolicy.Id,
                DestinationAddressId = 2,
                IsEnabled = true,
                Priority = 1,
                DistributionModeEnum = TransferDistributionMode.Fair
            },
            new DestinationTransferRule
            {
                SourceTransferPolicyId = fairPolicy.Id,
                DestinationAddressId = 3,
                IsEnabled = true,
                Priority = 2,
                DistributionModeEnum = TransferDistributionMode.Fair
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var result = await sut.RunAutomaticWorkflowForSourceAsync(
            1,
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        Assert.Equal(2, result.Count);

        var to2 = result.Single(x => x.DestinationAddressId == 2);
        var to3 = result.Single(x => x.DestinationAddressId == 3);

        Assert.Equal(5m, to2.AmountKwh);
        Assert.Equal(5m, to3.AmountKwh);

        Assert.All(result, x => Assert.Equal(TransferStatus.Planned, x.TransferStatusEnum));
        Assert.All(result, x => Assert.Equal(TriggerType.Auto, x.TriggerTypeEnum));
    }

    [Fact]
    public async Task RunAutomaticAllocationAsync_Priority_FillsFirstDestinationFirst()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Db;

        SeedAddresses(db, 1, 2, 3);

        db.DailyEnergyBalances.AddRange(
            Balance(1, 1, "2026-04-05", surplus: 10m, deficit: 0m),
            Balance(2, 2, "2026-04-05", surplus: 0m, deficit: 3m),
            Balance(3, 3, "2026-04-05", surplus: 0m, deficit: 10m));

        var priorityPolicy = new SourceTransferPolicy
        {
            Id = 1,
            SourceAddressId = 1,
            DistributionModeEnum = TransferDistributionMode.Priority,
            IsEnabled = true,
        };

        db.SourceTransferPolicies.Add(priorityPolicy);
        db.DestinationTransferRules.AddRange(
            new DestinationTransferRule
            {
                SourceTransferPolicyId = priorityPolicy.Id,
                DestinationAddressId = 2,
                IsEnabled = true,
                Priority = 1,
                DistributionModeEnum = TransferDistributionMode.Priority
            },
            new DestinationTransferRule
            {
                SourceTransferPolicyId = priorityPolicy.Id,
                DestinationAddressId = 3,
                IsEnabled = true,
                Priority = 2,
                DistributionModeEnum = TransferDistributionMode.Priority
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var result = await sut.RunAutomaticWorkflowForSourceAsync(
            1,
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        Assert.Equal(2, result.Count);

        var to2 = result.Single(x => x.DestinationAddressId == 2);
        var to3 = result.Single(x => x.DestinationAddressId == 3);

        Assert.Equal(3m, to2.AmountKwh);
        Assert.Equal(7m, to3.AmountKwh);
    }

    [Fact]
    public async Task RunAutomaticAllocationAsync_Weighted_SplitsByPercentage()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Db;

        SeedAddresses(db, 101, 102, 103);

        db.DailyEnergyBalances.AddRange(
            Balance(101, 101, "2026-04-05", surplus: 10m, deficit: 0m),
            Balance(102, 102, "2026-04-05", surplus: 0m, deficit: 10m),
            Balance(103, 103, "2026-04-05", surplus: 0m, deficit: 10m));

        var weightedPolicy = new SourceTransferPolicy
        {
            Id = 1,
            SourceAddressId = 101,
            DistributionModeEnum = TransferDistributionMode.Weighted,
            IsEnabled = true,
        };

        db.SourceTransferPolicies.Add(weightedPolicy);
        db.DestinationTransferRules.AddRange(
            new DestinationTransferRule
            {
                SourceTransferPolicyId = weightedPolicy.Id,
                DestinationAddressId = 102,
                IsEnabled = true,
                Priority = 1,
                WeightPercent = 70m,
                DistributionModeEnum = TransferDistributionMode.Weighted
            },
            new DestinationTransferRule
            {
                SourceTransferPolicyId = weightedPolicy.Id,
                DestinationAddressId = 103,
                IsEnabled = true,
                Priority = 2,
                WeightPercent = 30m,
                DistributionModeEnum = TransferDistributionMode.Weighted
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var result = await sut.RunAutomaticWorkflowForSourceAsync(
            101,
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        var relevant = result
            .Where(x => x.DestinationAddressId == 102 || x.DestinationAddressId == 103)
            .ToList();

        Assert.NotEmpty(relevant);

        var to2 = relevant.Where(x => x.DestinationAddressId == 102).Sum(x => x.AmountKwh);
        var to3 = relevant.Where(x => x.DestinationAddressId == 103).Sum(x => x.AmountKwh);
        var total = to2 + to3;

        Assert.True(to2 > to3);
        Assert.InRange(total, 9.999m, 10.001m);
    }

    [Fact]
    public async Task RunAutomaticAllocationAsync_DoesNotDuplicateExecutedTransfersOnSecondRun()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Db;

        SeedAddresses(db, 1, 2);

        db.DailyEnergyBalances.AddRange(
            Balance(1, 1, "2026-04-05", surplus: 5m, deficit: 0m),
            Balance(2, 2, "2026-04-05", surplus: 0m, deficit: 5m));

        var dedupePolicy = new SourceTransferPolicy
        {
            Id = 1,
            SourceAddressId = 1,
            DistributionModeEnum = TransferDistributionMode.Fair,
            IsEnabled = true,
        };

        db.SourceTransferPolicies.Add(dedupePolicy);
        db.DestinationTransferRules.Add(
            new DestinationTransferRule
            {
                SourceTransferPolicyId = dedupePolicy.Id,
                DestinationAddressId = 2,
                IsEnabled = true,
                Priority = 1,
                DistributionModeEnum = TransferDistributionMode.Fair
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var first = await sut.RunAutomaticWorkflowForSourceAsync(
            1,
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        var second = await sut.RunAutomaticWorkflowForSourceAsync(
            1,
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        Assert.Single(first);
        Assert.Single(second);

        var persisted = await db.TransferWorkflows.ToListAsync();
        Assert.Single(persisted);
        Assert.Equal(5m, persisted[0].AmountKwh);
    }

    [Fact]
    public async Task RunAutomaticAllocationAsync_WithSqlite_DoesNotThrowTranslationErrors()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Db;

        SeedAddresses(db, 1, 2);

        db.DailyEnergyBalances.AddRange(
            Balance(1, 1, "2026-04-05", surplus: 2m, deficit: 0m),
            Balance(2, 2, "2026-04-05", surplus: 0m, deficit: 2m));

        var sqlitePolicy = new SourceTransferPolicy
        {
            Id = 1,
            SourceAddressId = 1,
            DistributionModeEnum = TransferDistributionMode.Fair,
            IsEnabled = true,
        };

        db.SourceTransferPolicies.Add(sqlitePolicy);
        db.DestinationTransferRules.Add(
            new DestinationTransferRule
            {
                SourceTransferPolicyId = sqlitePolicy.Id,
                DestinationAddressId = 2,
                IsEnabled = true,
                Priority = 1,
                DistributionModeEnum = TransferDistributionMode.Fair
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var ex = await Record.ExceptionAsync(() =>
            sut.RunAutomaticWorkflowAsync(new DateOnly(2026, 4, 5), CancellationToken.None));

        Assert.Null(ex);
    }

    private static TransferWorkflowScheduledService CreateSut(
        VnmDbContext db,
        TransferDistributionMode defaultMode)
    {
        var options = new TestOptionsMonitor<TransferWorkflowOptions>(
            new TransferWorkflowOptions
            {
                Enabled = true,
                PollIntervalSeconds = 60,
               
            });

        return new TransferWorkflowScheduledService(
            db,
            NullLogger<TransferWorkflowScheduledService>.Instance);
    }

    private static void SeedAddresses(VnmDbContext db, params int[] ids)
    {
        foreach (var id in ids)
        {
            db.Addresses.Add(new Address
            {
                Id = id,
                Country = "RO",
                County = "Iasi",
                City = $"City{id}",
                Street = $"Street{id}",
                StreetNumber = $"{id}",
                PostalCode = $"700{id:000}"
            });

            db.InverterInfos.Add(new InverterInfo
            {
                Id = id,
                AddressId = id,
                Manufacturer = "Test",
                Model = "Test",
                SerialNumber = $"INV-{id}"
            });
        }
    }

    private static DailyEnergyBalance Balance(
        int addressId,
        int inverterInfoId,
        string day,
        decimal surplus,
        decimal deficit)
    {
        var net = surplus - deficit;

        return new DailyEnergyBalance
        {
            AddressId = addressId,
            InverterInfoId = inverterInfoId,
            Day = UtcDay(day),
            ProducedKwh = surplus,
            ConsumedKwh = deficit,
            NetKwh = net,
            NetPerAddressKwh = net,
            SurplusKwh = surplus,
            DeficitKwh = deficit,
            CalculatedAtUtc = DateTime.UtcNow,
            Status = "Computed"
        };
    }

    private static DateTime UtcDay(string yyyyMmDd)
    {
        var d = DateOnly.Parse(yyyyMmDd);
        return DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; private set; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public VnmDbContext Db { get; }

        private SqliteFixture(SqliteConnection connection, VnmDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public static async Task<SqliteFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<VnmDbContext>()
                .UseSqlite(connection)
                .EnableSensitiveDataLogging()
                .Options;

            var db = new VnmDbContext(options);
            await db.Database.EnsureCreatedAsync();

            return new SqliteFixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
