using AppHost.Extensions;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http;

var builder = DistributedApplication.CreateBuilder(args);

var uiPort = UiOptions.FromConfiguration(builder.Configuration).Ports.Dev;
var autoOpenUi = builder.Configuration.GetValue("AppHost:AutoOpenUi", true);

var prereq = builder.AddPrereqCheck();

var db = builder.AddVnmDatabaseInfrastructure();
db.SqlServer.WaitForCompletion(prereq);

// RabbitMQ setup for MeterIngestionWeb and other services. High prefix keeps it after primary app resources.
builder.AddRabbitMQ("res08-rabbitmq")
	.WithLifetime(ContainerLifetime.Persistent)
	.WithContainerName("vnm-rabbitmq")
	.WithManagementPlugin()
	.WaitForCompletion(prereq);

// 1) Setup resource
var initialSetup = builder.AddInitialSetup(db)
	.WaitForCompletion(prereq);

// 2) UI resource
var ui = builder.AddNpmApp("res02-ui-frontend", "../../../UI", "dev")
	.WithHttpEndpoint(targetPort: uiPort, port: uiPort, name: "http", isProxied: false)
	.WaitForCompletion(initialSetup);

// 3..6) Backend services resources (Dashboard, MeterIngestion, InverterSimulator, IdentityProvider)
var web = builder.AddVnmWebApps();
web.WireUpDependencies(db.VnmDb, initialSetup);

var app = builder.Build();

if (autoOpenUi && builder.ExecutionContext.IsRunMode)
{
	_ = Task.Run(() => OpenUiWhenReadyAsync(uiPort));
}

app.Run();

static async Task OpenUiWhenReadyAsync(int uiPort)
{
	var uiUrl = $"http://localhost:{uiPort}";

	using var http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(2)
	};

	var deadline = DateTime.UtcNow.AddMinutes(2);
	while (DateTime.UtcNow < deadline)
	{
		try
		{
			using var response = await http.GetAsync(uiUrl);
			if ((int)response.StatusCode >= 200)
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = uiUrl,
					UseShellExecute = true
				});
				return;
			}
		}
		catch
		{
			// UI not reachable yet.
		}

		await Task.Delay(TimeSpan.FromSeconds(1));
	}
}
