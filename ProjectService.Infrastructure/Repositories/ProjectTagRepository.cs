using Microsoft.EntityFrameworkCore;
using ProjectService.Domain.Models;
using ProjectService.Domain.Repositories;
using ProjectService.Infrastructure.Database;

namespace ProjectService.Infrastructure.Repositories;

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