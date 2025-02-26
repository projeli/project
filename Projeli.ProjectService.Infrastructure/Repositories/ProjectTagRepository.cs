using Microsoft.EntityFrameworkCore;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.ProjectService.Infrastructure.Database;

namespace Projeli.ProjectService.Infrastructure.Repositories;

public class ProjectTagRepository(ProjectServiceDbContext database) : IProjectTagRepository
{
    public async Task<List<ProjectTag>> GetByTags(List<string> tags)
    {
        return await database.ProjectTags
            .AsNoTracking()
            .Where(tag => tags.Contains(tag.Name))
            .ToListAsync();
    }
}