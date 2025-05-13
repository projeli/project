using Projeli.ProjectService.Domain.Repositories;
using Projeli.ProjectService.Infrastructure.Repositories;

namespace Projeli.ProjectService.Api.Extensions;

public static class RepositoriesExtension
{
    public static void AddProjectServiceRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectTagRepository, ProjectTagRepository>();
        services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
    }
}