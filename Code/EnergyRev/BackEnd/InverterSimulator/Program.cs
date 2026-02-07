using InverterSimulator.Configuration;
using ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EventBusCore;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;


var builder = WebApplication.CreateBuilder(args);
// Configuration
builder.Services.Configure<InverterSimulatorOptions>(builder.Configuration.GetSection("InverterSimulator"));

// MVC
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddCheck("fail", () => HealthCheckResult.Unhealthy());
builder.AddServiceDefaults();
var app = builder.Build();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
 {
     c.SwaggerEndpoint("/swagger/v1/swagger.json", "InverterSimulator API V1");
     c.RoutePrefix = string.Empty; // Swagger UI at root
 });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseRouting();
app.MapControllers();
app.MapDefaultEndpoints(); // health, metrics, etc
app.Run();
