using AppHost.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var uiOptions = UiOptions.FromConfiguration(builder.Configuration);
var environmentName = builder.Configuration["DOTNET_ENVIRONMENT"]
	?? builder.Configuration["ASPNETCORE_ENVIRONMENT"]
	?? Environments.Development;

var uiPort = uiOptions.GetPort(environmentName);
var uiUrl = uiOptions.GetUrl(environmentName);
var autoOpenUi = builder.Configuration.GetValue("AppHost:AutoOpenUi", true);

var prereq = builder.AddPrereqCheck();

var db = builder.AddVnmDatabaseInfrastructure();
db.SqlServer.WaitForCompletion(prereq);

// RabbitMQ setup for MeterIngestion and other services. High prefix keeps it after primary app resources.
builder.AddRabbitMQ("res08-rabbitmq")
	.WithLifetime(ContainerLifetime.Persistent)
	.WithContainerName("vnm-rabbitmq")
	.WithManagementPlugin()
	.WaitForCompletion(prereq);

// 1) Setup resource
var initialSetup = builder.AddInitialSetup(db)
	.WaitForCompletion(prereq);

// 2) UI resource
var ui = builder.AddNpmApp("res02-ui-frontend", "../../UI", "dev")
	.WithHttpEndpoint(targetPort: uiPort, port: uiPort, name: "http", isProxied: false)
	.WaitForCompletion(initialSetup);

// 3..6) Backend service resources (Dashboard, MeterIngestion, InverterSimulator, IdentityProvider)
var web = builder.AddVnmWebApps();
web.WireUpDependencies(db.VnmDb, initialSetup);

var app = builder.Build();

builder.OpenUiWhenReady(uiUrl, autoOpenUi);

app.Run();
