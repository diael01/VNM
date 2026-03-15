var builder = DistributedApplication.CreateBuilder(args);

var uiPort = UiOptions.FromConfiguration(builder.Configuration).Ports.Dev;

builder.AddNpmApp("UI", "../../../UI", "dev")
	.WithHttpEndpoint(targetPort: uiPort, port: uiPort, name: "http", isProxied: false);

builder.AddProject<Projects.DashboardBFFWeb>("DashboardBFFWeb");
builder.AddProject<Projects.MeterIngestionWeb>("MeterIngestionWeb");
builder.AddProject<Projects.InverterSimulatorWeb>("InverterSimulatorWeb");
builder.AddProject<Projects.IdentityProviderWeb>("IdentityProviderWeb");

builder.Build().Run();
