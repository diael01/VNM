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
using EnergyManagement.Services.Transfers.Execution;
using Microsoft.Extensions.Options;
using Infrastructure.Options;
using Microsoft.Extensions.Configuration;

namespace Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAspNetIdentityService, AspNetIdentityService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IInverterInfoService, InverterInfoService>();
         services.AddScoped<IDailyBalanceCalculationService, DailyBalanceCalculationService>();
        services.AddScoped<ITransferRuleService, TransferRuleService>();
        services.AddScoped<ISourceTransferPolicyService, SourceTransferPolicyService>();
        services.AddScoped<ISourceTransferScheduleService, SourceTransferScheduleService>();
        services.AddScoped<ITransferWorkflowCrudService, TransferWorkflowCrudService>();

        // Register BFF redirect services
        services.AddScoped<IDashboardAddressRedirectService, DashboardAddressRedirectService>();
        services.AddScoped<IDashboardInverterRedirectService, DashboardInverterRedirectService>();
        services.AddScoped<IDashboardConsumptionRedirectService, DashboardConsumptionRedirectService>();
         services.AddScoped<IDashboardDailyBalanceRedirectService, DashboardDailyBalanceRedirectService>();
        services.AddScoped<IDashboardTransferRuleRedirectService, DashboardTransferRuleRedirectService>();
        services.AddScoped<IDashboardSourceTransferPolicyRedirectService, DashboardSourceTransferPolicyRedirectService>();
        services.AddScoped<IDashboardTransferWorkflowRedirectService, DashboardTransferWorkflowRedirectService>();

        services.AddScoped<IProviderSettlementService, ProviderSettlementService>();
        services.AddScoped<IAvailableBalanceService, AvailableBalanceService>();
        services.AddScoped<ITransferWorkflowService, TransferWorkflowService>();  
        services.AddScoped<ISettlementModeResolver, SettlementModeResolver>(); 
        services.AddScoped<ISettlementModeStrategy, EnergySettlementModeStrategy>(); 
        services.AddScoped<ISettlementModeStrategy, MoneySettlementModeStrategy>(); 

        services.AddScoped<ITransferExecutionService, TransferExecutionService>();       

        services.Configure<TransferExecutionSimulatorOptions>(
        configuration.GetSection("TransferExecutionSimulator"));
        services.AddHttpClient<ITransferExecutionAdapter, HttpTransferExecutionAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TransferExecutionSimulatorOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.BaseUrl))
                throw new InvalidOperationException("TransferExecutionSimulator:BaseUrl is not configured.");
            client.BaseAddress = new Uri(options.BaseUrl);
        });
        return services;
    }
}

