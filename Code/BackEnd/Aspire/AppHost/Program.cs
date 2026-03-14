var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DashboardBFFWeb>("DashboardBFFWeb");
builder.AddProject<Projects.MeterIngestionWeb>("MeterIngestionWeb");
builder.AddProject<Projects.InverterSimulatorWeb>("InverterSimulatorWeb");
builder.AddProject<Projects.IdentityProviderWeb>("IdentityProviderWeb");

builder.Build().Run();
