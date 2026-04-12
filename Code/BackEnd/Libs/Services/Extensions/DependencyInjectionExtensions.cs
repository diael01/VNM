using Microsoft.Extensions.DependencyInjection;
using Services.Authorization;
using Services.Inverter;
using Services.Identity;
using Services.Redirect;
using Services.Transfers;
using EnergyManagement.Services.Analytics;
using EnergyManagement.Services.Providers;
using EnergyManagement.Services.Transfers;
using EnergyManagement.Services.ModeSwitching;

namespace Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAspNetIdentityService, AspNetIdentityService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IInverterInfoService, InverterInfoService>();
         services.AddScoped<IDailyBalanceCalculationService, DailyBalanceCalculationService>();
        services.AddScoped<ITransferRuleService, TransferRuleService>();
        services.AddScoped<ITransferWorkflowCrudService, TransferWorkflowCrudService>();

        // Register BFF redirect services
        services.AddScoped<IDashboardAddressRedirectService, DashboardAddressRedirectService>();
        services.AddScoped<IDashboardInverterRedirectService, DashboardInverterRedirectService>();
        services.AddScoped<IDashboardConsumptionRedirectService, DashboardConsumptionRedirectService>();
         services.AddScoped<IDashboardDailyBalanceRedirectService, DashboardDailyBalanceRedirectService>();
        services.AddScoped<IDashboardTransferRuleRedirectService, DashboardTransferRuleRedirectService>();
        services.AddScoped<IDashboardTransferWorkflowRedirectService, DashboardTransferWorkflowRedirectService>();

        services.AddScoped<IProviderSettlementService, ProviderSettlementService>();
        services.AddScoped<IAvailableBalanceService, AvailableBalanceService>();
        services.AddScoped<ITransferWorkflowService, TransferWorkflowService>();  
        services.AddScoped<ISettlementModeResolver, SettlementModeResolver>(); 
        services.AddScoped<ISettlementModeStrategy, EnergySettlementModeStrategy>(); 
         services.AddScoped<ISettlementModeStrategy, MoneySettlementModeStrategy>(); 
        return services;
    }
}

