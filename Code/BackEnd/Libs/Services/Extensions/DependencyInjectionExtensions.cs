using Microsoft.Extensions.DependencyInjection;
using Services.Authorization;
using Services.Inverter;
using Services.Identity;
using Services.Redirect;
using Services.Meter;
using EnergyManagement.Services.Analytics;
using EnergyManagement.Services.Providers;
using EnergyManagement.Services.Transfers;
using EnergyManagement.Services.ModeSwitching;
using Services.Analytics;

namespace Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IInverterReadingService, InverterReadingService>();
        services.AddScoped<IConsumptionReadingService, ConsumptionReadingService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAspNetIdentityService, AspNetIdentityService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IInverterInfoService, InverterInfoService>();
         services.AddScoped<IDailyBalanceCalculationService, DailyBalanceCalculationService>();
         services.AddScoped<IDailyBalanceDBService, DailyBalanceDBService>();

        // Register BFF redirect services
        services.AddScoped<IDashboardAddressRedirectService, DashboardAddressRedirectService>();
        services.AddScoped<IDashboardInverterRedirectService, DashboardInverterRedirectService>();
        services.AddScoped<IDashboardConsumptionRedirectService, DashboardConsumptionRedirectService>();
         services.AddScoped<IDashboardDailyBalanceRedirectService, DashboardDailyBalanceRedirectService>();

        services.AddScoped<IProviderSettlementService, ProviderSettlementService>();
        services.AddScoped<IAvailableBalanceService, AvailableBalanceService>();
        services.AddScoped<ITransferWorkflowService, TransferWorkflowService>();  
        services.AddScoped<ISettlementModeResolver, SettlementModeResolver>(); 
        services.AddScoped<ISettlementModeStrategy, EnergySettlementModeStrategy>(); 
         services.AddScoped<ISettlementModeStrategy, MoneySettlementModeStrategy>(); 
        return services;
    }
}

