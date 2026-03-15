using ServiceDefaults;
using Scalar.AspNetCore;
using Serilog;
using EventBusCore;
using MeterIngestionWeb.Consumers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using InverterPolling.Services;
using Microsoft.Extensions.Options;
using Infrastructure.Polling;
using VNM.Infrastructure.Extensions;
using Repositories.Data;
using Repositories.CRUD.Extensions;
using Services.DependencyInjection;

// ---------------------
// Build the application
// ---------------------
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Enable direct local runs outside Aspire by loading development secrets before service registration.
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("VnmDb")))
{
    var password = builder.Configuration["SA_PASSWORD"]
        ?? builder.Configuration["Parameters:sql-password"];

    if (!string.IsNullOrWhiteSpace(password))
    {
        var host = builder.Configuration["SQL_HOST"] ?? "localhost";
        var port = builder.Configuration["SQL_PORT"] ?? "1433";

        builder.Configuration["ConnectionStrings:VnmDb"] =
            $"Server=tcp:{host},{port};Database=VNM;User ID=sa;Password={password};Encrypt=True;TrustServerCertificate=True;";
    }
}

// ---------------------
// Event Bus
// ---------------------
builder.Services.AddEventBus(builder.Configuration, typeof(MeterEventConsumer).Assembly);

// ---------------------
// Controllers & Swagger
// ---------------------
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// ---------------------
// Health Checks
// ---------------------
builder.Services.AddHealthChecks()
       .AddCheck("basic", () => HealthCheckResult.Healthy("Service is running"));

// ---------------------
// Configuration Options
// ---------------------
builder.Services.Configure<InverterPollingOptions>(
    builder.Configuration.GetSection("InverterPolling"));

// ---------------------
// HTTP client for polling
// ---------------------
builder.Services.AddHttpClient();

// ---------------------
// DbContext
// ---------------------
builder.Services.AddSqlServerDbContexts<VnmDbContext, VnmDbContext>(builder.Configuration);

builder.Services.AddRepositoriesCrud();
builder.Services.AddAppServices();

// ---------------------
// Inverter Poller Factory & Poller (singleton)
// ---------------------
builder.Services.AddInverterPolling();

// ---------------------
// Aspire / Service defaults
// ---------------------
builder.AddServiceDefaults();

builder.Services.AddJwtAuthentication(builder.Configuration);
// ---------------------
// Build the app
// ---------------------
var app = builder.Build();

// ---------------------
// Logging
// ---------------------
app.UseSerilogRequestLogging();

// ---------------------
// Swagger UI in Development
// ---------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MeterIngestion API V1");
        c.RoutePrefix = string.Empty; // Serve at root
    });
}

// ---------------------
// Middleware
// ---------------------
app.UseHttpsRedirection();
app.UseAuthorization();

// ---------------------
// Endpoints
// ---------------------
app.MapControllers();
app.MapDefaultEndpoints(); // health, metrics, etc

// ---------------------
// Run the app
// ---------------------
app.Run();

