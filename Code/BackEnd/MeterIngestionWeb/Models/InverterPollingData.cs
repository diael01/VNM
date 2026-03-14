using Microsoft.EntityFrameworkCore;
using InverterPolling.Services;

namespace InverterPolling.Data
{
    public class SolarDbContext : DbContext
    {
        public SolarDbContext(DbContextOptions<SolarDbContext> options) : base(options) { }

        public DbSet<InverterReading> InverterReadings => Set<InverterReading>();
    }
}
