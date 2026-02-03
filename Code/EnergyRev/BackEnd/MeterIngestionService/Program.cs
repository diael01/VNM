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


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventBus(builder.Configuration, typeof(MeterEventConsumer).Assembly);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddCheck("fail", () => HealthCheckResult.Unhealthy());

builder.Services.Configure<InverterPollingOptions>(
    builder.Configuration.GetSection("InverterPolling"));

builder.Services.AddSingleton<IInverterPollerFactory, InverterPollerFactory>();
builder.Services.AddSingleton<IInverterPoller>(sp =>
{
    var options = sp.GetRequiredService<IOptions<InverterPollingOptions>>().Value;
    var factory = sp.GetRequiredService<IInverterPollerFactory>();
    return factory.Create(options);
});
builder.Services.AddHttpClient();
builder.Services.AddHostedService<InverterPollingService>();

builder.Services.AddDbContext<SolarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SolarDb")));

builder.AddServiceDefaults();

var app = builder.Build();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MeterIngestion API V1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();

