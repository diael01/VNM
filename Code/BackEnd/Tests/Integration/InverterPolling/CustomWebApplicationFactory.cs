using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using InverterPolling.Services;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using Microsoft.AspNetCore.TestHost;
using Repositories.Data;
using System.Diagnostics;
using Repositories.Models;

namespace MeterIngestionWeb.IntegrationTests;

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
            services.RemoveAll<DbContextOptions<VnmDbContext>>();
            services.RemoveAll<IDbContextFactory<VnmDbContext>>();
            services.RemoveAll<VnmDbContext>();
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IInverterPoller>();

            // Build config temporar
            var sp = services.BuildServiceProvider();
            var configuration = sp.GetRequiredService<IConfiguration>();

            var baseConn = ResolveVnmDbConnectionString(configuration);

            //this is for Mac,on Win we use integrated security and skip the password
            //todo: abstract it
            //var password = configuration["Sql:Password"];
            //if (string.IsNullOrWhiteSpace(password))
            //    throw new InvalidOperationException("Sql:Password not found (user-secrets)");

            var csb = new SqlConnectionStringBuilder(baseConn)
            {
                InitialCatalog = IntegrationTestBase.TestDb,
                //Password = password,
            };

            services.AddDbContextFactory<VnmDbContext>(options =>
                options.UseSqlServer(csb.ConnectionString));
            services.AddDbContext<VnmDbContext>(options =>
                options.UseSqlServer(csb.ConnectionString));

            // Mock poller
            var pollerMock = new Mock<IInverterPoller>();
            pollerMock.Setup(p => p.PollAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InverterReading
                {
                    Power = 100,
                    Voltage = 230,
                    Current = 5,
                    Timestamp = DateTime.UtcNow,
                    Source = "Test"
                });

            services.AddSingleton(pollerMock.Object);
            services.AddHostedService<InverterPollingService>();
        });
    }

    private static string ResolveVnmDbConnectionString(IConfiguration configuration)
    {
        var fromConfig = configuration.GetConnectionString("VnmDb");
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            var csb = new SqlConnectionStringBuilder(fromConfig);
            if (string.IsNullOrWhiteSpace(csb.Password))
            {
                var pwd = ResolveSqlPassword(configuration);
                if (!string.IsNullOrWhiteSpace(pwd))
                {
                    csb.Password = pwd;
                }
            }
            return csb.ConnectionString;
        }

        var password = ResolveSqlPassword(configuration);
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "VnmDb connection is missing. Provide ConnectionStrings:VnmDb or set SA_PASSWORD (or Parameters:sql-password) in environment/user-secrets.");
        }

        return new SqlConnectionStringBuilder
        {
            DataSource = "localhost,1433",
            InitialCatalog = "VNM",
            UserID = "sa",
            Password = password,
            TrustServerCertificate = true,
        }.ConnectionString;
    }

    private static string? ResolveSqlPassword(IConfiguration configuration)
    {
        return configuration["Sql:Password"]
            ?? configuration["Parameters:sql-password"]
            ?? Environment.GetEnvironmentVariable("SA_PASSWORD")
            ?? TryReadDockerSaPassword("vnm-sqlserver");
    }

    private static string? TryReadDockerSaPassword(string containerName)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = $"inspect --format \"{{{{range .Config.Env}}}}{{{{println .}}}}{{{{end}}}}\" {containerName}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("SA_PASSWORD=", StringComparison.Ordinal))
                {
                    return line["SA_PASSWORD=".Length..];
                }
            }
        }
        catch
        {
            // Ignore and fall back to null.
        }

        return null;
    }
}

