using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace AppHost.Extensions;

public sealed record VnmDatabaseResources(
    IResourceBuilder<SqlServerServerResource> SqlServer,
    IResourceBuilder<SqlServerDatabaseResource> VnmDb);

public static class DatabaseHostingExtensions
{
    public static VnmDatabaseResources AddVnmDatabaseInfrastructure(this IDistributedApplicationBuilder builder)
    {
        // Use a configuration-backed parameter builder without adding a top-level dashboard resource.
        var sqlPassword = builder.CreateResourceBuilder(
            new ParameterResource(
                "sql-password",
                _ => ResolveSqlPassword(builder.Configuration),
                secret: true));

        var sqlServer = builder.AddSqlServer("res07-sqlserver", sqlPassword)
            .WithLifetime(ContainerLifetime.Persistent)
            .WithContainerName("vnm-sqlserver");

        // Create an internal VNM connection-string resource without a separate dashboard row.
        var vnmDb = builder.CreateResourceBuilder(
            new SqlServerDatabaseResource("VnmDb", "VNM", sqlServer.Resource));

        return new VnmDatabaseResources(sqlServer, vnmDb);
    }

    private static string ResolveSqlPassword(IConfiguration configuration)
    {
        var password = configuration["Parameters:sql-password"]
            ?? configuration["SA_PASSWORD"];

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new DistributedApplicationException(
                "SQL password is missing. Set Parameters:sql-password in AppHost user-secrets or SA_PASSWORD in the environment.");
        }

        return password;
    }
}
