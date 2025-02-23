using ProjectService.Domain.Repositories;
using ProjectService.Infrastructure.Repositories;

namespace ProjectService.Api.Extensions;

public static class RepositoriesExtension
{
    public static void AddProjectServiceRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectTagRepository, ProjectTagRepository>();
    }
}