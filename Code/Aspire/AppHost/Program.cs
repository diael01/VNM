using AppHost.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text;

var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.Configuration["Parameters:sql-password"]
    ?? builder.Configuration["SA_PASSWORD"];
var rabbitPassword = builder.Configuration["Parameters:res08-rabbitmq-password"]
    ?? builder.Configuration["RABBITMQ_DEFAULT_PASS"];

if (string.IsNullOrWhiteSpace(sqlPassword) || string.IsNullOrWhiteSpace(rabbitPassword))
{
    var message = new StringBuilder();
    message.AppendLine("Missing required local secrets for AppHost startup.");
    message.AppendLine("Run EasyRun first:");
    if (OperatingSystem.IsWindows())
        message.AppendLine("powershell -NoProfile -ExecutionPolicy Bypass -File .\\Setup\\easyrun.ps1");
    else
        message.AppendLine("bash ./Setup/easyRunMac.sh");
    message.AppendLine();
    message.AppendLine("Set them once using user-secrets:");

    if (string.IsNullOrWhiteSpace(sqlPassword))
    {
        message.AppendLine("dotnet user-secrets set \"Parameters:sql-password\" \"<your-sql-password>\" --project Aspire/AppHost/AppHost.csproj");
    }

    if (string.IsNullOrWhiteSpace(rabbitPassword))
    {
        message.AppendLine("dotnet user-secrets set \"Parameters:res08-rabbitmq-password\" \"<your-rabbit-password>\" --project Aspire/AppHost/AppHost.csproj");
    }

    throw new DistributedApplicationException(message.ToString());
}

var uiOptions = UiOptions.FromConfiguration(builder.Configuration);
var environmentName = builder.Configuration["DOTNET_ENVIRONMENT"]
    ?? builder.Configuration["ASPNETCORE_ENVIRONMENT"]
    ?? Environments.Development;

var uiPort = uiOptions.GetPort(environmentName);
var uiUrl = uiOptions.GetUrl(environmentName);
var autoOpenUi = builder.Configuration.GetValue("AppHost:AutoOpenUi", true);

var prereq = builder.AddPrereqCheck();
builder.AddCoverageDashboard();

var db = builder.AddVnmDatabaseInfrastructure();
db.SqlServer.WaitForCompletion(prereq);

// RabbitMQ setup for MeterIngestion and other services. High prefix keeps it after primary app resources.
var rabbitMq = builder.AddRabbitMQ("res08-rabbitmq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("vnm-rabbitmq")
    .WithManagementPlugin()
    .WaitForCompletion(prereq);

// 1) Setup resource
var initialSetup = builder.AddInitialSetup(db)
    .WaitForCompletion(prereq);

// 2) React UI resource
var startupDelaySeconds = builder.Configuration.GetValue<int>("StartupDelaySeconds", 120);

// Add delay resource
var delayResource = builder.AddExecutable(
    "res99-startup-delay",
    OperatingSystem.IsWindows() ? "powershell" : "bash",
    "../../Setup",
    OperatingSystem.IsWindows()
        ? new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "delay.ps1", startupDelaySeconds.ToString() }
        : new[] { "delayMac.sh", startupDelaySeconds.ToString() });
var ui = builder.AddNpmApp("res02-ui-frontend", "../../ReactUI", "dev")
    .WithHttpEndpoint(targetPort: uiPort, port: uiPort, name: "http", isProxied: false)
    .WaitForCompletion(delayResource);

// 3..6) Backend service resources (Dashboard, MeterIngestion, InverterSimulator, IdentityProvider)
var web = builder.AddVnmWebApps();
web.WireUpDependencies(db.VnmDb, rabbitMq, delayResource);

var app = builder.Build();

builder.OpenUiWhenReady(uiUrl, autoOpenUi);

app.Run();
