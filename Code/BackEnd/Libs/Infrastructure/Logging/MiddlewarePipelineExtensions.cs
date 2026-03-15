using Infrastructure.Exceptions;
using Infrastructure.Logging;
using Microsoft.AspNetCore.Builder;

namespace VNM.Infrastructure.Extensions;

public static class MiddlewarePipelineExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleWare>();
        return app;
    }

    public static IApplicationBuilder UseStructuredRequestLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<SerilogMiddleware>();
        return app;
    }
}
