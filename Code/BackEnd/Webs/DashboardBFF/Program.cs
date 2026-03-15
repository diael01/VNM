using ServiceDefaults;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using EventBusCore;
using Dashboard.Consumers;
using Serilog;
using VNM.Infrastructure.Extensions;
using Services.Auth;
using Services.Redirect;
using Repositories.Data;
using Repositories.CRUD.Extensions;
using Services.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.TryConfigureLocalVnmDbConnection();

// Platform
builder.AddServiceDefaults();
builder.Services.AddEventBus(builder.Configuration, typeof(DashboardConsumer).Assembly);

// MVC / API
builder.Services.AddControllers();
builder.Services.AddSwager();
builder.Services.AddHealthChecks()
    .AddCheck("basic", () => HealthCheckResult.Healthy("Service is running"));

//Infrastructure
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddBffAuthentication(builder.Configuration);
builder.Services.AddDownstreamServiceClients(builder.Configuration);
builder.Services.AddSqlServerDbContext<VnmDbContext>(builder.Configuration);
builder.Services.AddRepositoriesCrud();
builder.Services.AddAppServices();

//Application Services
builder.Services.AddBffApplicationServices();

var app = builder.Build();
app.UseGlobalExceptionHandling();
app.UseStructuredRequestLogging();

//Swagger
app.UseSwagerInDevelopment();

app.UseHttpsRedirection();
app.UseCors(CorsExtensions.FrontendCorsPolicy);
app.UseBffSecurity();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();