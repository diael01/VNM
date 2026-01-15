var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.EventBusMock>("EventBusMock");
//.WithEndpoint(name:"https-api", scheme: "https",port: 7140,targetPort: 7140,isProxied: false);

builder.AddProject<Projects.MeterIngestionService>("MeterIngestionService");
//.WithEndpoint(name:"https-api", scheme: "https",port: 7142,targetPort: 7142,isProxied: false);

builder.AddProject<Projects.MultiSiteEnergyPlatform>("MultiSiteEnergyPlatform");
//.WithEndpoint(name:"https-api", scheme: "https",port: 7144,targetPort: 7144,isProxied: false);

builder.AddProject<Projects.StubServices>("StubServices");
//.WithEndpoint(name:"https-api", scheme: "https",port: 7146,targetPort: 7146,isProxied: false);

builder.Build().Run();
