using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AppHost.Extensions;

public sealed record VnmWebResources(
    IResourceBuilder<ProjectResource> DashboardBff,
    IResourceBuilder<ProjectResource> MeterIngestion,
    IResourceBuilder<ProjectResource> InverterSimulator,
    IResourceBuilder<ProjectResource> IdentityProvider);

public static class WebHostingExtensions
{
    public static VnmWebResources AddVnmWebApps(this IDistributedApplicationBuilder builder)
    {
        var dashboard = builder.AddProject<Projects.DashboardBFF>("res03-DashboardBFF");

        var meterIngestion = builder.AddProject<Projects.MeterIngestion>("res04-MeterIngestion");

        var inverterSimulator = builder.AddProject<Projects.InverterSimulator>("res05-InverterSimulator");

        var identityProvider = builder.AddProject<Projects.IdentityProvider>("res06-IdentityProvider");

        return new VnmWebResources(dashboard, meterIngestion, inverterSimulator, identityProvider);
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

        web.MeterIngestion
            .WithReference(vnmDb)
            .WithReference(rabbitMq)
            .WaitForCompletion(initialSetup);

        web.InverterSimulator
            .WaitForCompletion(initialSetup);

        web.IdentityProvider
            .WaitForCompletion(initialSetup);
    }
}
