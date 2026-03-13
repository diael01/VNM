using ServiceDefaults;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using EventBusCore;
using Dashboard.Consumers;
using Serilog;
using VNM.Infrastructure.Extensions;
using DashboardBff.Services.Auth;
using DashboardBff.Endpoints;
using DashboardBff.Services.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventBus(builder.Configuration, typeof(DashboardConsumer).Assembly);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddCheck("fail", () => HealthCheckResult.Unhealthy());

builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddBffAuthentication(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();


builder.Services.AddHttpClient("inverter-api", client =>
{
    client.BaseAddress = new Uri("https://localhost:5286/");
});
builder.Services.AddScoped<IDashboardService, DashboardService>();


var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dashboard BFF API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("ReactDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapDashboardEndpoints();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();