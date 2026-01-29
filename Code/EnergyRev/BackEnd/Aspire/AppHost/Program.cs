var builder = DistributedApplication.CreateBuilder(args);
//builder.AddProject<Projects.EventBusClient>("EventBusclient");

builder.AddProject<Projects.MeterIngestionService>("MeterIngestionService");

builder.AddProject<Projects.MultiSiteEnergyPlatform>("MultiSiteEnergyPlatform");

builder.Build().Run();
