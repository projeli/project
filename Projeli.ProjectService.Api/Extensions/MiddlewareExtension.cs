using Projeli.ProjectService.Api.Middlewares;

namespace Projeli.ProjectService.Api.Extensions;

public static class MiddlewareExtension
{
    public static void UseProjectServiceMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<DatabaseExceptionMiddleware>();
        builder.UseMiddleware<HttpExceptionMiddleware>();
    }
}