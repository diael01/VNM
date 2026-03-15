using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VNM.Infrastructure.Extensions;

/// <summary>
/// Provides reusable SQL Server DbContext registrations.
/// </summary>
public static class SqlServerDataAccessExtensions
{
    /// <summary>
    /// Registers both a DbContextFactory and a DbContext using the same SQL Server connection string.
    /// </summary>
    public static IServiceCollection AddSqlServerDbContexts<TFactoryContext, TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionName = "VnmDb")
        where TFactoryContext : DbContext
        where TDbContext : DbContext
    {
        var connectionString = GetRequiredConnectionString(configuration, connectionName);

        // When both generic args are the same context type, avoid registering AddDbContext + AddDbContextFactory
        // with conflicting options lifetimes. Instead, expose scoped context instances from the factory.
        if (typeof(TFactoryContext) == typeof(TDbContext))
        {
            services.AddDbContextFactory<TFactoryContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<TDbContext>(sp =>
                (TDbContext)(object)sp.GetRequiredService<IDbContextFactory<TFactoryContext>>().CreateDbContext());
            return services;
        }

        services.AddDbContextFactory<TFactoryContext>(options => options.UseSqlServer(connectionString));
        services.AddDbContext<TDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }

    /// <summary>
    /// Registers a DbContext that uses SQL Server from a named connection string.
    /// </summary>
    public static IServiceCollection AddSqlServerDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionName = "VnmDb")
        where TContext : DbContext
    {
        var connectionString = GetRequiredConnectionString(configuration, connectionName);
        services.AddDbContext<TContext>(options => options.UseSqlServer(connectionString));
        return services;
    }

    /// <summary>
    /// Registers a DbContextFactory that uses SQL Server from a named connection string.
    /// </summary>
    public static IServiceCollection AddSqlServerDbContextFactory<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionName = "VnmDb")
        where TContext : DbContext
    {
        var connectionString = GetRequiredConnectionString(configuration, connectionName);
        services.AddDbContextFactory<TContext>(options => options.UseSqlServer(connectionString));
        return services;
    }

    private static string GetRequiredConnectionString(IConfiguration configuration, string connectionName)
    {
        var connectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionName}' is missing. Configure 'ConnectionStrings:{connectionName}' via Aspire, environment variables, or user secrets.");
        }

        return connectionString;
    }
}
