using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using InverterPolling.Data;
using InverterPolling.Services;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using Microsoft.AspNetCore.TestHost;

namespace MeterIngestionService.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // IMPORTANT: folosim appsettings-ul aplicației reale
            config.AddJsonFile("appsettings.json", optional: false);
            config.AddUserSecrets<Program>();
            config.AddEnvironmentVariables();
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<SolarDbContext>>();
            services.RemoveAll<IDbContextFactory<SolarDbContext>>();
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IInverterPoller>();

            // Build config temporar
            var sp = services.BuildServiceProvider();
            var configuration = sp.GetRequiredService<IConfiguration>();

            var baseConn = configuration.GetConnectionString("SolarDb");
            if (string.IsNullOrWhiteSpace(baseConn))
                throw new InvalidOperationException("SolarDb connection string missing");

            var password = configuration["Sql:Password"];
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Sql:Password not found (user-secrets)");

            var csb = new SqlConnectionStringBuilder(baseConn)
            {
                InitialCatalog = IntegrationTestBase.TestDb,
                Password = password,
                TrustServerCertificate = true
            };

            services.AddDbContextFactory<SolarDbContext>(options =>
                options.UseSqlServer(csb.ConnectionString)
            );

            // Mock poller
            var pollerMock = new Mock<IInverterPoller>();
            pollerMock.Setup(p => p.PollAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InverterReading
                {
                    PowerW = 100,
                    VoltageV = 230,
                    CurrentA = 5,
                    TimestampUtc = DateTime.UtcNow,
                    Source = "Test"
                });

            services.AddSingleton(pollerMock.Object);
            services.AddHostedService<InverterPollingService>();
        });
    }
}
