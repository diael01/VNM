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
builder.Services.AddDbContext<SolarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SolarDb")));

// ---------------------
// Inverter Poller Factory & Poller (singleton)
// ---------------------
builder.Services.AddSingleton<IInverterPollerFactory, InverterPollerFactory>();
builder.Services.AddSingleton<IInverterPoller>(sp =>
{
    var options = sp.GetRequiredService<IOptions<InverterPollingOptions>>().Value;
    var factory = sp.GetRequiredService<IInverterPollerFactory>();
    return factory.Create(options);
});

// ---------------------
// Hosted service (polling)
// ---------------------
builder.Services.AddHostedService<InverterPollingService>();

// ---------------------
// Aspire / Service defaults
// ---------------------
builder.AddServiceDefaults();

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

//create database if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();
    db.Database.EnsureCreated();
}

// ---------------------
// Run the app
// ---------------------
app.Run();
