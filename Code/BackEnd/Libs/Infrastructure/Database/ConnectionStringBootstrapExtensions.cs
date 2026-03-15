using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace VNM.Infrastructure.Extensions;

public static class ConnectionStringBootstrapExtensions
{
    public static WebApplicationBuilder TryConfigureLocalVnmDbConnection(this WebApplicationBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("VnmDb")))
        {
            return builder;
        }

        var password = builder.Configuration["SA_PASSWORD"]
            ?? builder.Configuration["Parameters:sql-password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            return builder;
        }

        var host = builder.Configuration["SQL_HOST"] ?? "localhost";
        var port = builder.Configuration["SQL_PORT"] ?? "1433";

        builder.Configuration["ConnectionStrings:VnmDb"] =
            $"Server=tcp:{host},{port};Database=VNM;User ID=sa;Password={password};Encrypt=True;TrustServerCertificate=True;";

        return builder;
    }
}