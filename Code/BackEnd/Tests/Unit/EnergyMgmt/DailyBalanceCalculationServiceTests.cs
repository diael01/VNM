using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;
using EnergyManagement.Services.Analytics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Repositories.Models;
using Xunit;

namespace EnergyManagement.Tests.Services.Analytics
{
    public class DailyBalanceCalculationServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<VnmDbContext> _options;

        public DailyBalanceCalculationServiceTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<VnmDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private VnmDbContext CreateContext()
        {
            return new VnmDbContext(_options);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_NoReadings_ReturnsZero()
        {
            using var db = CreateContext();

            db.Addresses.Add(new Address { Id = 1 });
            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);
            var day = DateOnly.FromDateTime(DateTime.UtcNow);

            var result = await service.CalculateDailyBalancesAsync(1, day, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.LocationId);
            Assert.Equal(0m, result.ProducedKwh);
            Assert.Equal(0m, result.ConsumedKwh);
            Assert.Equal(0m, result.NetKwh);
            Assert.Equal(0m, result.SurplusKwh);
            Assert.Equal(0m, result.DeficitKwh);

            var count = await db.DailyEnergyBalances.CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_ProducedGreaterThanConsumed_ReturnsSurplus()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);

            db.Addresses.Add(new Address { Id = 1 });
            db.InverterInfos.Add(new InverterInfo
            {
                Id = 10,
                AddressId = 1
            });

            db.InverterReadings.Add(new InverterReading
            {
                Id = 100,
                InverterId = 10,
                Timestamp = now,
                Power = 4000
            });

            db.ConsumptionReadings.Add(new ConsumptionReading
            {
                Id = 200,
                LocationId = 1,
                Timestamp = now,
                Power = 1000m
            });

            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(1, day, CancellationToken.None);

            Assert.Equal(1.00m, result.ProducedKwh);
            Assert.Equal(0.25m, result.ConsumedKwh);
            Assert.Equal(0.75m, result.NetKwh);
            Assert.Equal(0.75m, result.SurplusKwh);
            Assert.Equal(0m, result.DeficitKwh);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_ConsumedGreaterThanProduced_ReturnsDeficit()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);

            db.Addresses.Add(new Address { Id = 1 });
            db.InverterInfos.Add(new InverterInfo
            {
                Id = 10,
                AddressId = 1
            });

            db.InverterReadings.Add(new InverterReading
            {
                Id = 101,
                InverterId = 10,
                Timestamp = now,
                Power = 1000m
            });

            db.ConsumptionReadings.Add(new ConsumptionReading
            {
                Id = 201,
                LocationId = 1,
                Timestamp = now,
                Power = 4000m
            });

            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(1, day, CancellationToken.None);

            Assert.Equal(0.25m, result.ProducedKwh);
            Assert.Equal(1.00m, result.ConsumedKwh);
            Assert.Equal(-0.75m, result.NetKwh);
            Assert.Equal(0m, result.SurplusKwh);
            Assert.Equal(0.75m, result.DeficitKwh);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_WhenBalanceAlreadyExists_UpdatesExistingRow()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);
            var dayStart = day.ToDateTime(TimeOnly.MinValue);

            db.Addresses.Add(new Address { Id = 1 });
            db.InverterInfos.Add(new InverterInfo
            {
                Id = 10,
                AddressId = 1
            });

            db.DailyEnergyBalances.Add(new DailyEnergyBalance
            {
                Id = 999,
                LocationId = 1,
                Day = dayStart,
                ProducedKwh = 123m,
                ConsumedKwh = 456m,
                NetKwh = -333m,
                SurplusKwh = 0m,
                DeficitKwh = 333m,
                CalculatedAtUtc = now.AddHours(-1),
                Status = "Old"
            });

            db.InverterReadings.Add(new InverterReading
            {
                Id = 102,
                InverterId = 10,
                Timestamp = now,
                Power = 4000m
            });

            db.ConsumptionReadings.Add(new ConsumptionReading
            {
                Id = 202,
                LocationId = 1,
                Timestamp = now,
                Power = 1000m
            });

            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(1, day, CancellationToken.None);

            var balances = await db.DailyEnergyBalances
                .Where(x => x.LocationId == 1)
                .ToListAsync();

            Assert.Single(balances);

            var saved = balances.Single();

            Assert.Equal(999, saved.Id);
            Assert.Equal(1.00m, saved.ProducedKwh);
            Assert.Equal(0.25m, saved.ConsumedKwh);
            Assert.Equal(0.75m, saved.NetKwh);
            Assert.Equal(0.75m, saved.SurplusKwh);
            Assert.Equal(0m, saved.DeficitKwh);
            Assert.NotEqual("Old", saved.Status);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_IgnoresReadingsOutsideRequestedDay()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);
            var previousDay = day.AddDays(-1).ToDateTime(new TimeOnly(12, 0));
            var nextDay = day.AddDays(1).ToDateTime(new TimeOnly(12, 0));
            var todayReadingTime = day.ToDateTime(new TimeOnly(10, 0));

            db.Addresses.Add(new Address { Id = 1 });
            db.InverterInfos.Add(new InverterInfo
            {
                Id = 10,
                AddressId = 1
            });

            db.InverterReadings.AddRange(
                new InverterReading
                {
                    Id = 103,
                    InverterId = 10,
                    Timestamp = previousDay,
                    Power = 5000m
                },
                new InverterReading
                {
                    Id = 104,
                    InverterId = 10,
                    Timestamp = todayReadingTime,
                    Power = 4000m
                },
                new InverterReading
                {
                    Id = 105,
                    InverterId = 10,
                    Timestamp = nextDay,
                    Power = 6000m
                });

            db.ConsumptionReadings.AddRange(
                new ConsumptionReading
                {
                    Id = 203,
                    LocationId = 1,
                    Timestamp = previousDay,
                    Power = 5000m
                },
                new ConsumptionReading
                {
                    Id = 204,
                    LocationId = 1,
                    Timestamp = todayReadingTime,
                    Power = 1000m
                },
                new ConsumptionReading
                {
                    Id = 205,
                    LocationId = 1,
                    Timestamp = nextDay,
                    Power = 6000m
                });

            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(1, day, CancellationToken.None);

            Assert.Equal(1.00m, result.ProducedKwh);
            Assert.Equal(0.25m, result.ConsumedKwh);
            Assert.Equal(0.75m, result.NetKwh);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_OnlyUsesRequestedAddress()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);

            db.Addresses.AddRange(
                new Address { Id = 1 },
                new Address { Id = 2 });

            db.InverterInfos.AddRange(
                new InverterInfo { Id = 10, AddressId = 1 },
                new InverterInfo { Id = 20, AddressId = 2 });

            db.InverterReadings.AddRange(
                new InverterReading
                {
                    Id = 106,
                    InverterId = 10,
                    Timestamp = now,
                    Power = 4000m
                },
                new InverterReading
                {
                    Id = 107,
                    InverterId = 20,
                    Timestamp = now,
                    Power = 9000m
                });

            db.ConsumptionReadings.AddRange(
                new ConsumptionReading
                {
                    Id = 206,
                    LocationId = 1,
                    Timestamp = now,
                    Power = 1000m
                },
                new ConsumptionReading
                {
                    Id = 207,
                    LocationId = 2,
                    Timestamp = now,
                    Power = 8000m
                });

            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(1, day, CancellationToken.None);

            Assert.Equal(1, result.LocationId);
            Assert.Equal(1.00m, result.ProducedKwh);
            Assert.Equal(0.25m, result.ConsumedKwh);
            Assert.Equal(0.75m, result.NetKwh);
        }

        [Fact]
        public async Task CalculateDailyBalancesForAllAddressesAsync_CreatesOneRowPerAddress()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);

            db.Addresses.AddRange(
                new Address { Id = 1 },
                new Address { Id = 2 },
                new Address { Id = 3 });

            db.InverterInfos.AddRange(
                new InverterInfo { Id = 10, AddressId = 1 },
                new InverterInfo { Id = 20, AddressId = 2 },
                new InverterInfo { Id = 30, AddressId = 3 });

            db.InverterReadings.AddRange(
                new InverterReading { Id = 108, InverterId = 10, Timestamp = now, Power = 4000m },
                new InverterReading { Id = 109, InverterId = 20, Timestamp = now, Power = 2000m },
                new InverterReading { Id = 110, InverterId = 30, Timestamp = now, Power = 0m });

            db.ConsumptionReadings.AddRange(
                new ConsumptionReading { Id = 208, LocationId = 1, Timestamp = now, Power = 1000m },
                new ConsumptionReading { Id = 209, LocationId = 2, Timestamp = now, Power = 3000m },
                new ConsumptionReading { Id = 210, LocationId = 3, Timestamp = now, Power = 500m });

            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesForAllAddressesAsync(day, CancellationToken.None);

            Assert.Equal(3, result.Count);

            var dbRows = await db.DailyEnergyBalances
                .OrderBy(x => x.LocationId)
                .ToListAsync();

            Assert.Equal(3, dbRows.Count);
            Assert.Equal(1, dbRows[0].LocationId);
            Assert.Equal(2, dbRows[1].LocationId);
            Assert.Equal(3, dbRows[2].LocationId);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_CalledTwice_DoesNotInsertDuplicateRow()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);

            db.Addresses.Add(new Address { Id = 1 });
            db.InverterInfos.Add(new InverterInfo
            {
                Id = 10,
                AddressId = 1
            });

            db.InverterReadings.Add(new InverterReading
            {
                Id = 111,
                InverterId = 10,
                Timestamp = now,
                Power = 4000m
            });

            db.ConsumptionReadings.Add(new ConsumptionReading
            {
                Id = 211,
                LocationId = 1,
                Timestamp = now,
                Power = 1000m
            });

            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            await service.CalculateDailyBalancesAsync(1, day, CancellationToken.None);
            await service.CalculateDailyBalancesAsync(1, day, CancellationToken.None);

            var rows = await db.DailyEnergyBalances
                .Where(x => x.LocationId == 1)
                .ToListAsync();

            Assert.Single(rows);
        }
    }
}