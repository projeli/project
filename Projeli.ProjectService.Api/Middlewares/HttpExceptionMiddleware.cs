using System.Text.Json;
using Projeli.Shared.Application.Exceptions.Http;

namespace Projeli.ProjectService.Api.Middlewares;

public class HttpExceptionMiddleware(RequestDelegate next, ILogger<DatabaseExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (HttpException exception)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)exception.StatusCode;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    status = exception.StatusCode,
                    message = exception.Message,
                }));
                await context.Response.CompleteAsync();
            }
        }
    }
}