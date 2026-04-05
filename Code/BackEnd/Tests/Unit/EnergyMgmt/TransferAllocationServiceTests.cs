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

public class TransferAllocationServiceTests
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

        db.TransferRules.AddRange(
            new TransferRule
            {
                SourceAddressId = 1,
                DestinationAddressId = 2,
                IsEnabled = true,
                Priority = 1,
                DistributionMode = (int)TransferDistributionMode.Fair
            },
            new TransferRule
            {
                SourceAddressId = 1,
                DestinationAddressId = 3,
                IsEnabled = true,
                Priority = 2,
                DistributionMode = (int)TransferDistributionMode.Fair
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var result = await sut.RunAutomaticAllocationAsync(
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        Assert.Equal(2, result.Count);

        var to2 = result.Single(x => x.DestinationAddressId == 2);
        var to3 = result.Single(x => x.DestinationAddressId == 3);

        Assert.Equal(5m, to2.AllocatedKwh);
        Assert.Equal(5m, to3.AllocatedKwh);

        Assert.All(result, x => Assert.Equal((int)TransferStatus.Executed, x.Status));
        Assert.All(result, x => Assert.Equal((int)TriggerType.Auto, x.TriggerType));
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

        db.TransferRules.AddRange(
            new TransferRule
            {
                SourceAddressId = 1,
                DestinationAddressId = 2,
                IsEnabled = true,
                Priority = 1,
                DistributionMode = (int)TransferDistributionMode.Priority
            },
            new TransferRule
            {
                SourceAddressId = 1,
                DestinationAddressId = 3,
                IsEnabled = true,
                Priority = 2,
                DistributionMode = (int)TransferDistributionMode.Priority
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var result = await sut.RunAutomaticAllocationAsync(
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        Assert.Equal(2, result.Count);

        var to2 = result.Single(x => x.DestinationAddressId == 2);
        var to3 = result.Single(x => x.DestinationAddressId == 3);

        Assert.Equal(3m, to2.AllocatedKwh);
        Assert.Equal(7m, to3.AllocatedKwh);
    }

    [Fact]
    public async Task RunAutomaticAllocationAsync_Weighted_SplitsByPercentage()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var db = fixture.Db;

        SeedAddresses(db, 1, 2, 3);

        db.DailyEnergyBalances.AddRange(
            Balance(1, 1, "2026-04-05", surplus: 10m, deficit: 0m),
            Balance(2, 2, "2026-04-05", surplus: 0m, deficit: 10m),
            Balance(3, 3, "2026-04-05", surplus: 0m, deficit: 10m));

        db.TransferRules.AddRange(
            new TransferRule
            {
                SourceAddressId = 1,
                DestinationAddressId = 2,
                IsEnabled = true,
                Priority = 1,
                WeightPercent = 70m,
                DistributionMode = (int)TransferDistributionMode.Weighted
            },
            new TransferRule
            {
                SourceAddressId = 1,
                DestinationAddressId = 3,
                IsEnabled = true,
                Priority = 2,
                WeightPercent = 30m,
                DistributionMode = (int)TransferDistributionMode.Weighted
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var result = await sut.RunAutomaticAllocationAsync(
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        Assert.Equal(2, result.Count);

        var to2 = result.Single(x => x.DestinationAddressId == 2);
        var to3 = result.Single(x => x.DestinationAddressId == 3);

        Assert.Equal(7m, to2.AllocatedKwh);
        Assert.Equal(3m, to3.AllocatedKwh);
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

        db.TransferRules.Add(
            new TransferRule
            {
                SourceAddressId = 1,
                DestinationAddressId = 2,
                IsEnabled = true,
                Priority = 1,
                DistributionMode = (int)TransferDistributionMode.Fair
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var first = await sut.RunAutomaticAllocationAsync(
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        var second = await sut.RunAutomaticAllocationAsync(
            new DateOnly(2026, 4, 5),
            CancellationToken.None);

        Assert.Single(first);
        Assert.Empty(second);

        var persisted = await db.TransferExecutions.ToListAsync();
        Assert.Single(persisted);
        Assert.Equal(5m, persisted[0].AllocatedKwh);
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

        db.TransferRules.Add(
            new TransferRule
            {
                SourceAddressId = 1,
                DestinationAddressId = 2,
                IsEnabled = true,
                Priority = 1,
                DistributionMode = (int)TransferDistributionMode.Fair
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db, TransferDistributionMode.Fair);

        var ex = await Record.ExceptionAsync(() =>
            sut.RunAutomaticAllocationAsync(new DateOnly(2026, 4, 5), CancellationToken.None));

        Assert.Null(ex);
    }

    private static TransferAllocationService CreateSut(
        VnmDbContext db,
        TransferDistributionMode defaultMode)
    {
        var options = new TestOptionsMonitor<TransferAllocationOptions>(
            new TransferAllocationOptions
            {
                Enabled = true,
                IntervalMinutes = 15,
                DistributionMode = defaultMode
            });

        return new TransferAllocationService(
            db,
            NullLogger<TransferAllocationService>.Instance,
            options);
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