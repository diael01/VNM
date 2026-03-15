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
        var dashboard = builder.AddProject<Projects.DashboardBFFWeb>("res03-DashboardBFFWeb");

        var meterIngestion = builder.AddProject<Projects.MeterIngestionWeb>("res04-MeterIngestionWeb");

        var inverterSimulator = builder.AddProject<Projects.InverterSimulatorWeb>("res05-InverterSimulatorWeb");

        var identityProvider = builder.AddProject<Projects.IdentityProviderWeb>("res06-IdentityProviderWeb");

        return new VnmWebResources(dashboard, meterIngestion, inverterSimulator, identityProvider);
    }

    public static void WireUpDependencies(
        this VnmWebResources web,
        IResourceBuilder<SqlServerDatabaseResource> vnmDb,
        IResourceBuilder<ExecutableResource> initialSetup)
    {
        web.DashboardBff
            .WithReference(vnmDb)
            .WaitForCompletion(initialSetup);

        web.MeterIngestion
            .WithReference(vnmDb)
            .WaitForCompletion(initialSetup);

        web.InverterSimulator
            .WaitForCompletion(initialSetup);

        web.IdentityProvider
            .WaitForCompletion(initialSetup);
    }
}
