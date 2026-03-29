using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AppHost.Extensions;

public sealed record VnmWebResources(
    IResourceBuilder<ProjectResource> DashboardBff,
    IResourceBuilder<ProjectResource> EnergyManagement,
    IResourceBuilder<ProjectResource> Simulators,
    IResourceBuilder<ProjectResource> IdentityProvider);

public static class WebHostingExtensions
{
    public static VnmWebResources AddVnmWebApps(this IDistributedApplicationBuilder builder)
    {
        var dashboard = builder.AddProject<Projects.DashboardBFF>("res03-DashboardBFF");


        var energyManagement = builder.AddProject<Projects.EnergyManagement>("res04-EnergyManagement");

        var simulators = builder.AddProject<Projects.Simulators>("res05-Simulators");

        var identityProvider = builder.AddProject<Projects.IdentityProvider>("res06-IdentityProvider");

        return new VnmWebResources(dashboard, energyManagement, simulators, identityProvider);
    }

    public static void WireUpDependencies(
        this VnmWebResources web,
        IResourceBuilder<SqlServerDatabaseResource> vnmDb,
        IResourceBuilder<RabbitMQServerResource> rabbitMq,
        IResourceBuilder<ExecutableResource> initialSetup)
    {
        web.DashboardBff
            .WithReference(vnmDb)
            .WithReference(rabbitMq)
            .WaitForCompletion(initialSetup);

        web.EnergyManagement
            .WithReference(vnmDb)
            .WithReference(rabbitMq)
            .WaitForCompletion(initialSetup);

        web.Simulators
            .WaitForCompletion(initialSetup);

        web.IdentityProvider
            .WaitForCompletion(initialSetup);
    }
}
