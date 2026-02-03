var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MultiSiteEnergyPlatform>("MultiSiteEnergyPlatform");
builder.AddProject<Projects.MeterIngestionService>("MeterIngestionService");
builder.AddProject<Projects.InverterSimulator>("InverterSimulator");

builder.Build().Run();
