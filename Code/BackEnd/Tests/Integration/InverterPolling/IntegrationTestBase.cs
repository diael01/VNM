using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MeterIngestionWeb.IntegrationTests;

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

        var connectionString = ResolveVnmDbConnectionString(config);

        //todo: abstract it to work on WIn and Mac (on Win we use integrated security and skip the password, on Mac we need it from user-secrets or env)
        //var password = config["Sql:Password"] ?? Environment.GetEnvironmentVariable("SA_PASSWORD");
        // if (string.IsNullOrWhiteSpace(password))
        //     throw new InvalidOperationException("Sql:Password not found");
        // -----------------------------
        // Build connection string for test DB
        // -----------------------------
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = TestDb,
            //Password = password,
        };
        var testDbConnString = builder.ConnectionString;

        // -----------------------------
        // Reset the test database
        // -----------------------------
        await ResetTestDatabase(testDbConnString);

    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task ResetTestDatabase(string testDbConnString)
    {
        await using var connection = new SqlConnection(testDbConnString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();

        cmd.CommandText = $@"
            IF DB_ID(N'{TestDb}') IS NULL
                THROW 50001, 'Database VNM_TEST is missing. Run AppHost and execute initial-setup first.', 1;

            USE [{TestDb}];

            IF OBJECT_ID(N'dbo.InverterReadings', N'U') IS NULL
                THROW 50002, 'Table dbo.InverterReadings is missing. Run AppHost and execute initial-setup first.', 1;

            DELETE FROM [dbo].[InverterReadings];";

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

