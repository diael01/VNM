var builder = DistributedApplication.CreateBuilder(args);
//builder.AddProject<Projects.EventBusClient>("EventBusclient");

builder.AddProject<Projects.MeterIngestionService>("MeterIngestionService");

builder.AddProject<Projects.MultiSiteEnergyPlatform>("MultiSiteEnergyPlatform");

builder.AddProject<Projects.StubServices>("StubServices");

builder.Build().Run();
