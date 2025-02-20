using ProjectService.Application.Services.Interfaces;

namespace ProjectService.Api.Extensions;

public static class ServiceExtension
{
    public static void AddProjectServiceServices(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, Application.Services.ProjectService>();
    }
}