using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceDefaults;

using System.Diagnostics.Metrics;


var builder = WebApplication.CreateBuilder(args);
var meter = new Meter("eventbus-mock"); 
builder.Services.AddSingleton(meter);
builder.Services.AddControllers();
builder.AddServiceDefaults();
builder.Services.AddSwaggerGen();


var app = builder.Build();
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Enable Swagger only in Development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventBusMock API V1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseRouting();
app.MapControllers();
app.Run();


