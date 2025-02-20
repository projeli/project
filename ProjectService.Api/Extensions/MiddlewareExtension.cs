using ProjectService.Api.Middlewares;

namespace ProjectService.Api.Extensions;

public static class MiddlewareExtension
{
    public static void UseAtlaMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<DatabaseExceptionMiddleware>();
        builder.UseMiddleware<HttpExceptionMiddleware>();
    }
}