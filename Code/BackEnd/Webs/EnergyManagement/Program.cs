using Infrastructure.Validation;
using ServiceDefaults;
using EventBusCore;
using EnergyManagement.Consumers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Infrastructure.Polling;
using VNM.Infrastructure.Extensions;
using Repositories.CRUD.Extensions;
using Services.DependencyInjection;
using Services.Profiles;
using Repositories.Models;
using Polling.Services.Auth;

// ---------------------
// Build the application
// ---------------------
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Enable direct local runs outside Aspire by loading development secrets before service registration.
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.TryConfigureLocalVnmDbConnection();

// ---------------------
// Event Bus
// ---------------------
builder.Services.AddEventBus(builder.Configuration, typeof(MeterEventConsumer).Assembly);

// ---------------------
// Controllers, Swagger, and Validation
// ---------------------
builder.Services.AddControllers();
builder.Services.AddSwager();
builder.Services.AddAppValidators();

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

// Consumption Polling Options
builder.Services.Configure<ConsumptionPolling.Services.ConsumptionPollingOptions>(
    builder.Configuration.GetSection("ConsumptionPolling"));

// ---------------------
// HTTP client for polling
// ---------------------
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAccessTokenProvider, ClientCredentialsAccessTokenProvider>();

// ---------------------
// AutoMapper
// ---------------------
builder.Services.AddAutoMapper(typeof(AddressProfile).Assembly);
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
builder.Services.AddConsumptionPolling();

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
app.UseGlobalExceptionHandling();
app.UseStructuredRequestLogging();

// ---------------------
// Swagger UI in Development
// ---------------------
//Swagger
app.UseSwagerInDevelopment();

// ---------------------
// Middleware
// ---------------------
app.UseHttpsRedirection();
app.UseAuthentication();
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

