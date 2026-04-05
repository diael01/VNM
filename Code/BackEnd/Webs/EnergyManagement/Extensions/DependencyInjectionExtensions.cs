
using EnergyManagement.Services.Analytics;
using EnergyManagement.Services.ModeSwitching;
using EnergyManagement.Services.Providers;
using EnergyManagement.Services.Transfers;
using Infrastructure.Options;


namespace EnergyManagement.Extensions;

public static class DailyBalanceServiceCollectionExtensions
{
    public static IServiceCollection AddDailyBalanceComputation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DailyBalanceComputationOptions>(
            configuration.GetSection("DailyBalanceComputation"));

         services.AddHostedService<DailyBalanceComputationBackgroundService>();

        return services;
    }
}