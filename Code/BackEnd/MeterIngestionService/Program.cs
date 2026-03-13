using ServiceDefaults;
using Scalar.AspNetCore;
using Serilog;
using EventBusCore;
using MeterIngestionService.Consumers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using InverterPolling.Services;
using InverterPolling.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Infrastructure.Polling;
using VNM.Infrastructure.Extensions;

// ---------------------
// Build the application
// ---------------------
var builder = WebApplication.CreateBuilder(args);

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
// DbContext (Scoped)
// ---------------------
builder.Services.AddDbContextFactory<SolarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SolarDb")));

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
    builder.Configuration.AddUserSecrets<Program>();
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
// Ensure database exists on startup
// ---------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();
    db.Database.Migrate(); // Applies migrations or creates DB if missing
}

// ---------------------
// Run the app
// ---------------------
app.Run();
