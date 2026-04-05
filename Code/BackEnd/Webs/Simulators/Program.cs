using ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EventBusCore;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VNM.Infrastructure.Extensions;
using Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<InverterSimulatorOptions>(
    builder.Configuration.GetSection("InverterSimulator"));
builder.Services.Configure<ConsumptionSimulatorOptions>(
    builder.Configuration.GetSection("ConsumptionSimulator"));

// MVC
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwager();
builder.Services.AddHealthChecks()
    .AddCheck("basic", () => HealthCheckResult.Healthy("Service is running"));

builder.AddServiceDefaults();
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.UseGlobalExceptionHandling();
app.UseStructuredRequestLogging();

//Swagger
app.UseSwagerInDevelopment();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
