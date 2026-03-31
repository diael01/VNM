using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagementWeb.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;

    // Removed static TestDb; use Factory.TestDbName for per-test isolation

    private static bool _isDbInitialized = false;
    private static readonly object _lock = new();

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }
    public async Task InitializeAsync() {// Apply EF Core migrations to ensure all tables exist
    lock (_lock)
    {
        if (_isDbInitialized) return;
        _isDbInitialized = true;
    }

    var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()// for SA_PASSWORD
        .Build();

    var connectionString = ResolveVnmDbConnectionString(config);

    var builder = new SqlConnectionStringBuilder(connectionString)
    {
        InitialCatalog = Factory.TestDbName,
    };
    var testDbConnString = builder.ConnectionString;

    // Ensure the test database exists
    await EnsureTestDatabaseCreated(testDbConnString, Factory.TestDbName);

    // Apply EF Core migrations to ensure all tables exist
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Repositories.Models.VnmDbContext>();
            db.Database.Migrate();
        }

    // Now that schema exists, reset test data
    await ResetTestDatabase(testDbConnString);
    }

    // Removed old expression-bodied DisposeAsync; cleanup logic is now in the async method below.
    public async Task DisposeAsync()
    {
        // Drop the test database after the test completes
        var dbName = Factory.TestDbName;
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();
        var connectionString = ResolveVnmDbConnectionString(config);
        var masterConnStr = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ConnectionString;
        await using var connection = new SqlConnection(masterConnStr);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            IF DB_ID(N'{dbName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{dbName}];
            END";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ResetTestDatabase(string testDbConnString)
    {
        await using var connection = new SqlConnection(testDbConnString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();

        // Extract db name from connection string
        var dbName = new SqlConnectionStringBuilder(testDbConnString).InitialCatalog;

        cmd.CommandText = $@"
            IF DB_ID(N'{dbName}') IS NULL
                THROW 50001, 'Database {dbName} is missing. Run AppHost and execute initial-setup first.', 1;

            USE [{dbName}];

            IF OBJECT_ID(N'dbo.InverterReadings', N'U') IS NULL
                THROW 50002, 'Table dbo.InverterReadings is missing. Run AppHost and execute initial-setup first.', 1;

            DELETE FROM [dbo].[InverterReadings];";

        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task EnsureTestDatabaseCreated(string testDbConnString, string dbName)
    {
        // Connect to master to create the DB if it doesn't exist
        var masterConnStr = new SqlConnectionStringBuilder(testDbConnString) { InitialCatalog = "master" }.ConnectionString;
        await using var connection = new SqlConnection(masterConnStr);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            IF DB_ID(N'{dbName}') IS NULL
                CREATE DATABASE [{dbName}];";
        await cmd.ExecuteNonQueryAsync();
    }

    private static string ResolveVnmDbConnectionString(IConfiguration config)
    {
        var fromConfig = config.GetConnectionString("VnmDb");
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            var csb = new SqlConnectionStringBuilder(fromConfig);
            if (string.IsNullOrWhiteSpace(csb.Password))
            {
                var pwd = ResolveSqlPassword(config);
                if (!string.IsNullOrWhiteSpace(pwd))
                {
                    csb.Password = pwd;
                }
            }
            return csb.ConnectionString;
        }

        var password = ResolveSqlPassword(config);
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "VnmDb connection is missing. Provide ConnectionStrings:VnmDb or set SA_PASSWORD (or Parameters:sql-password) in environment/user-secrets.");
        }

        var port = config["SqlServer:Port"] ?? "1433";
        return new SqlConnectionStringBuilder
        {
            DataSource = $"localhost,{port}",
            InitialCatalog = "VNM",
            UserID = "sa",
            Password = password,
            TrustServerCertificate = true,
        }.ConnectionString;
    }

    private static string? ResolveSqlPassword(IConfiguration config)
    {
        return config["Sql:Password"]
            ?? config["Parameters:sql-password"]
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

