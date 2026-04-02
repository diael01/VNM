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
        public async Task CalculateDailyBalancesAsync_ProducedGreaterThanConsumed_ReturnsSurplus()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);

            var address = new Address { Id = 1 };
            db.Addresses.Add(address);
            var inverterInfo = new InverterInfo { Id = 10, AddressId = 1, Address = address, Model = "M1", Manufacturer = "Manu", SerialNumber = "SN1" };
            db.InverterInfos.Add(inverterInfo);
            await db.SaveChangesAsync();
            db.InverterReadings.Add(new InverterReading
            {
                Id = 100,
                InverterInfoId = 10,
                InverterInfo = inverterInfo,
                Timestamp = now,
                Power = 4000
            });
            db.ConsumptionReadings.Add(new ConsumptionReading
            {
                Id = 200,
                InverterInfoId = 10,
                InverterInfo = inverterInfo,
                Timestamp = now,
                Power = 1000m
            });
            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(10, day, CancellationToken.None);

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

            var address = new Address { Id = 1 };
            db.Addresses.Add(address);
            var inverterInfo = new InverterInfo { Id = 10, AddressId = 1, Address = address, Model = "M1", Manufacturer = "Manu", SerialNumber = "SN1" };
            db.InverterInfos.Add(inverterInfo);
            await db.SaveChangesAsync();
            db.InverterReadings.Add(new InverterReading
            {
                Id = 101,
                InverterInfoId = 10,
                InverterInfo = inverterInfo,
                Timestamp = now,
                Power = 1000m
            });
            db.ConsumptionReadings.Add(new ConsumptionReading
            {
                Id = 201,
                InverterInfoId = 10,
                InverterInfo = inverterInfo,
                Timestamp = now,
                Power = 4000m
            });
            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(10, day, CancellationToken.None);

            Assert.Equal(0.25m, result.ProducedKwh);
            Assert.Equal(1.00m, result.ConsumedKwh);
            Assert.Equal(-0.75m, result.NetKwh);
            Assert.Equal(0m, result.SurplusKwh);
            Assert.Equal(0.75m, result.DeficitKwh);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_NoReadings_ReturnsZero()
        {
            using var db = CreateContext();

            var address = new Address { Id = 1 };
            db.Addresses.Add(address);
            var inverterInfo = new InverterInfo { Id = 10, AddressId = 1, Address = address, Model = "M1", Manufacturer = "Manu", SerialNumber = "SN1" };
            db.InverterInfos.Add(inverterInfo);
            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);
            var day = DateOnly.FromDateTime(DateTime.UtcNow);

            var result = await service.CalculateDailyBalancesAsync(10, day, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.AddressId);
            Assert.Equal(0m, result.ProducedKwh);
            Assert.Equal(0m, result.ConsumedKwh);
            Assert.Equal(0m, result.NetKwh);
            Assert.Equal(0m, result.SurplusKwh);
            Assert.Equal(0m, result.DeficitKwh);

            var count = await db.DailyEnergyBalances.CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_WhenBalanceAlreadyExists_UpdatesExistingRow()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);
            var dayStart = day.ToDateTime(TimeOnly.MinValue);

            var address = new Address { Id = 1 };
            db.Addresses.Add(address);
            var inverterInfo = new InverterInfo { Id = 10, AddressId = 1, Address = address };
            db.InverterInfos.Add(inverterInfo);
            db.DailyEnergyBalances.Add(new DailyEnergyBalance
            {
                Id = 999,
                AddressId = 1,
                InverterInfoId = 10,
                InverterInfo = inverterInfo,
                Address = address,
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
                InverterInfoId = 10,
                InverterInfo = inverterInfo,
                Timestamp = now,
                Power = 4000m
            });
            db.ConsumptionReadings.Add(new ConsumptionReading
            {
                Id = 202,
                InverterInfoId = 10,
                InverterInfo = inverterInfo,
                Timestamp = now,
                Power = 1000m
            });
            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(10, day, CancellationToken.None);

            var balances = await db.DailyEnergyBalances
                .Where(x => x.AddressId == 1)
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

            var address = new Address { Id = 1 };
            db.Addresses.Add(address);
            var inverterInfo = new InverterInfo { Id = 10, AddressId = 1, Address = address };
            db.InverterInfos.Add(inverterInfo);
            db.InverterReadings.AddRange(
                new InverterReading
                {
                    Id = 103,
                    InverterInfoId = 10,
                    InverterInfo = inverterInfo,
                    Timestamp = previousDay,
                    Power = 5000m
                },
                new InverterReading
                {
                    Id = 104,
                    InverterInfoId = 10,
                    InverterInfo = inverterInfo,
                    Timestamp = todayReadingTime,
                    Power = 4000m
                },
                new InverterReading
                {
                    Id = 105,
                    InverterInfoId = 10,
                    InverterInfo = inverterInfo,
                    Timestamp = nextDay,
                    Power = 6000m
                });
            db.ConsumptionReadings.AddRange(
                new ConsumptionReading
                {
                    Id = 203,
                    InverterInfoId = 10,
                    InverterInfo = inverterInfo,
                    Timestamp = previousDay,
                    Power = 5000m
                },
                new ConsumptionReading
                {
                    Id = 204,
                    InverterInfoId = 10,
                    InverterInfo = inverterInfo,
                    Timestamp = todayReadingTime,
                    Power = 1000m
                },
                new ConsumptionReading
                {
                    Id = 205,
                    InverterInfoId = 10,
                    InverterInfo = inverterInfo,
                    Timestamp = nextDay,
                    Power = 6000m
                });
            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(10, day, CancellationToken.None);

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

            var address1 = new Address { Id = 1 };
            var address2 = new Address { Id = 2 };
            db.Addresses.AddRange(address1, address2);
            var inverterInfo1 = new InverterInfo { Id = 10, AddressId = 1, Address = address1 };
            var inverterInfo2 = new InverterInfo { Id = 20, AddressId = 2, Address = address2 };
            db.InverterInfos.AddRange(inverterInfo1, inverterInfo2);
            db.InverterReadings.AddRange(
                new InverterReading
                {
                    Id = 106,
                    InverterInfoId = 10,
                    InverterInfo = inverterInfo1,
                    Timestamp = now,
                    Power = 4000m
                },
                new InverterReading
                {
                    Id = 107,
                    InverterInfoId = 20,
                    InverterInfo = inverterInfo2,
                    Timestamp = now,
                    Power = 9000m
                });
            db.ConsumptionReadings.AddRange(
                new ConsumptionReading
                {
                    Id = 206,
                    InverterInfoId = 10,
                    InverterInfo = inverterInfo1,
                    Timestamp = now,
                    Power = 1000m
                },
                new ConsumptionReading
                {
                    Id = 207,
                    InverterInfoId = 20,
                    InverterInfo = inverterInfo2,
                    Timestamp = now,
                    Power = 8000m
                });
            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesAsync(10, day, CancellationToken.None);

            Assert.Equal(1, result.AddressId);
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

            var address1 = new Address { Id = 1 };
            var address2 = new Address { Id = 2 };
            var address3 = new Address { Id = 3 };
            db.Addresses.AddRange(address1, address2, address3);
            var inverterInfo1 = new InverterInfo { Id = 10, AddressId = 1, Address = address1 };
            var inverterInfo2 = new InverterInfo { Id = 20, AddressId = 2, Address = address2 };
            var inverterInfo3 = new InverterInfo { Id = 30, AddressId = 3, Address = address3 };
            db.InverterInfos.AddRange(inverterInfo1, inverterInfo2, inverterInfo3);
            db.InverterReadings.AddRange(
                new InverterReading { Id = 108, InverterInfoId = 10, InverterInfo = inverterInfo1, Timestamp = now, Power = 4000m },
                new InverterReading { Id = 109, InverterInfoId = 20, InverterInfo = inverterInfo2, Timestamp = now, Power = 2000m },
                new InverterReading { Id = 110, InverterInfoId = 30, InverterInfo = inverterInfo3, Timestamp = now, Power = 0m });
            db.ConsumptionReadings.AddRange(
                new ConsumptionReading { Id = 208, InverterInfoId = 10, InverterInfo = inverterInfo1, Timestamp = now, Power = 1000m },
                new ConsumptionReading { Id = 209, InverterInfoId = 20, InverterInfo = inverterInfo2, Timestamp = now, Power = 3000m },
                new ConsumptionReading { Id = 210, InverterInfoId = 30, InverterInfo = inverterInfo3, Timestamp = now, Power = 500m });
            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            var result = await service.CalculateDailyBalancesForAllInvertersAsync(day, CancellationToken.None);

            Assert.Equal(3, result.Count);

            var dbRows = await db.DailyEnergyBalances
                .OrderBy(x => x.AddressId)
                .ToListAsync();

            Assert.Equal(3, dbRows.Count);
            Assert.Equal(1, dbRows[0].AddressId);
            Assert.Equal(2, dbRows[1].AddressId);
            Assert.Equal(3, dbRows[2].AddressId);
        }

        [Fact]
        public async Task CalculateDailyBalancesAsync_CalledTwice_DoesNotInsertDuplicateRow()
        {
            using var db = CreateContext();

            var now = DateTime.UtcNow;
            var day = DateOnly.FromDateTime(now);

            var address = new Address { Id = 1 };
            db.Addresses.Add(address);
            var inverterInfo = new InverterInfo { Id = 10, AddressId = 1, Address = address, Model = "M1", Manufacturer = "Manu", SerialNumber = "SN1" };
            db.InverterInfos.Add(inverterInfo);
            db.InverterReadings.Add(new InverterReading
            {
                Id = 111,
                InverterInfoId = 10,
                InverterInfo = inverterInfo,
                Timestamp = now,
                Power = 4000m
            });
            db.ConsumptionReadings.Add(new ConsumptionReading
            {
                Id = 211,
                InverterInfoId = 10,
                InverterInfo = inverterInfo,
                Timestamp = now,
                Power = 1000m
            });
            await db.SaveChangesAsync();

            var service = new DailyBalanceCalculationService(db);

            await service.CalculateDailyBalancesAsync(10, day, CancellationToken.None);
            await service.CalculateDailyBalancesAsync(10, day, CancellationToken.None);

            var rows = await db.DailyEnergyBalances
                .Where(x => x.AddressId == 1)
                .ToListAsync();

            Assert.Single(rows);
        }
    }
}