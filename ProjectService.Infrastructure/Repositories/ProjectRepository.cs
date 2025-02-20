using Microsoft.EntityFrameworkCore;
using ProjectService.Domain.Models;
using ProjectService.Domain.Repositories;
using ProjectService.Infrastructure.Database;

namespace ProjectService.Infrastructure.Repositories;

public class ProjectRepository(ProjectServiceDbContext database) : IProjectRepository
{
    public async Task<Project?> GetById(Ulid id)
    {
        return await database.Projects.FindAsync(id);
    }

    public async Task<Project?> GetBySlug(string slug)
    {
        return await database.Projects.FirstOrDefaultAsync(project => project.Slug == slug);
    }

    public async Task<Project?> Create(Project project)
    {
        var createdProject = await database.Projects.AddAsync(project);
        await database.SaveChangesAsync();
        return createdProject.Entity;
    }
}