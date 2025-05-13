using Projeli.ProjectService.Application.Services.Interfaces;

namespace Projeli.ProjectService.Api.Extensions;

public static class ServiceExtension
{
    public static void AddProjectServiceServices(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, Application.Services.ProjectService>();
        services.AddScoped<IProjectMemberService, Application.Services.ProjectMemberService>();
    }
}