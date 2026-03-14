using ServiceDefaults;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using EventBusCore;
using Dashboard.Consumers;
using Serilog;
using VNM.Infrastructure.Extensions;
using Services.Auth;
using Services.Redirect;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.CRUD.Extensions;
using Services.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
// Platform
builder.AddServiceDefaults();
builder.Services.AddEventBus(builder.Configuration, typeof(DashboardConsumer).Assembly);

// MVC / API
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck("fail", () => HealthCheckResult.Unhealthy());

//Infrastructure
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddBffAuthentication(builder.Configuration);
builder.Services.AddDownstreamServiceClients(builder.Configuration);
builder.Services.AddDbContext<VnmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SolarDb")));
builder.Services.AddRepositoriesCrud();
builder.Services.AddAppServices();

//Application Services
builder.Services.AddBffApplicationServices();

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
app.UseCors(CorsExtensions.FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();