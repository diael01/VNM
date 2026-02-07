using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using InverterPolling.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MeterIngestionService.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;

    public const string TestDb = "VNM_TEST";

    private static bool _isDbInitialized = false;
    private static readonly object _lock = new();

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }

    public async Task InitializeAsync()
    {
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

        var connectionString = config.GetConnectionString("SolarDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'SolarDb' not found in appsettings.json");


       var password = config["Sql:Password"] ?? Environment.GetEnvironmentVariable("SA_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Sql:Password not found");
        // -----------------------------
        // Build connection string for test DB
        // -----------------------------
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = TestDb,
            Password = password,
        };
        var testDbConnString = builder.ConnectionString;

        // -----------------------------
        // Reset the test database
        // -----------------------------
        await ResetTestDatabase(testDbConnString);

        // -----------------------------
        // Apply migrations using scoped factory
        // -----------------------------
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SolarDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task ResetTestDatabase(string testDbConnString)
    {
        // Use master DB to drop/create test DB
        var masterConnBuilder = new SqlConnectionStringBuilder(testDbConnString)
        {
            InitialCatalog = "master"
        };
        var masterConnString = masterConnBuilder.ConnectionString;

        await using var connection = new SqlConnection(masterConnString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();

        cmd.CommandText = $@"
            IF EXISTS (SELECT * FROM sys.databases WHERE name = '{TestDb}')
            BEGIN
                ALTER DATABASE [{TestDb}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{TestDb}];
            END

            CREATE DATABASE [{TestDb}];";

        await cmd.ExecuteNonQueryAsync();
    }
}
