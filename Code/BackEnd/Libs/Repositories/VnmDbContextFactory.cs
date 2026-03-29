using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Repositories.Models
{
    public class VnmDbContextFactory : IDesignTimeDbContextFactory<VnmDbContext>
    {
        public VnmDbContext CreateDbContext(string[] args)
        {
            // Build config from appsettings and user secrets
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<VnmDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<VnmDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new VnmDbContext(optionsBuilder.Options);
        }
    }
}
