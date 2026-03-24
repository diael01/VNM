using Microsoft.Extensions.DependencyInjection;
using Repositories.CRUD.Interfaces;
using Repositories.CRUD.Repositories;

namespace Repositories.CRUD.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositoriesCrud(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IInverterReadingRepository, InverterReadingRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IInverterInfoRepository, InverterInfoRepository>();

        services.AddScoped<IAspNetRoleRepository, AspNetRoleRepository>();
        services.AddScoped<IAspNetRoleClaimRepository, AspNetRoleClaimRepository>();
        services.AddScoped<IAspNetUserRepository, AspNetUserRepository>();
        services.AddScoped<IAspNetUserClaimRepository, AspNetUserClaimRepository>();
        return services;
    }
}
