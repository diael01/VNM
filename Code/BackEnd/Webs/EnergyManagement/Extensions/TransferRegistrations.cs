
using EnergyManagement.Services.Analytics;
using EnergyManagement.Services.ModeSwitching;
using EnergyManagement.Services.Providers;
using EnergyManagement.Services.Transfers;


namespace EnergyManagement.Extensions;

public static class DailyBalanceServiceCollectionExtensions
{
    public static IServiceCollection AddDailyBalanceComputation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DailyBalanceComputationOptions>(
            configuration.GetSection("DailyBalanceComputation"));

        services.AddScoped<IDailyBalanceCalculationService, DailyBalanceCalculationService>();
        services.AddHostedService<DailyBalanceComputationBackgroundService>();
        services.AddScoped<IDailyBalanceCalculationService, DailyBalanceCalculationService>();     
        services.AddScoped<IProviderSettlementService, ProviderSettlementService>();
        services.AddScoped<IAvailableBalanceService, AvailableBalanceService>();
        services.AddScoped<ITransferService, TransferService>();  
        services.AddScoped<ISettlementModeResolver, SettlementModeResolver>(); 
        services.AddScoped<ISettlementModeStrategy, EnergySettlementModeStrategy>(); 
         services.AddScoped<ISettlementModeStrategy, MoneySettlementModeStrategy>(); 


        return services;
    }
}