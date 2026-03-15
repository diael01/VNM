using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net;

namespace Infrastructure.Exceptions
{
    public class ExceptionMiddleWare
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ExceptionMiddleWare> logger;
        public ExceptionMiddleWare(RequestDelegate nex, ILogger<ExceptionMiddleWare> log)
        {
            next = nex;
            logger = log;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            switch (exception)
            {
                //case ContabApiException:                
                //var ex = exception as ContabApiException;
                //await context.Response.WriteAsync(ex!.ToJson());
                //return;
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                case ArgumentException or InvalidOperationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
            }
            //logger.LogError(exception.Message);
            var msg = exception.InnerException != null ? exception.InnerException.Message : exception.Message;
            if (msg != null)
                logger.LogError(msg);
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                context.Response.StatusCode,
                msg,

            }));
        }
    }
}

