using Microsoft.EntityFrameworkCore;
using ProjectService.Domain.Models;
using ProjectService.Domain.Repositories;
using ProjectService.Domain.Results;
using ProjectService.Infrastructure.Database;

namespace ProjectService.Infrastructure.Repositories;

public class ProjectRepository(ProjectServiceDbContext database) : IProjectRepository
{
    public async Task<PagedResult<Project>> Get(string query, ProjectOrder order, int page, int pageSize, string? fromUserId = null, string? userId = null)
    {
        var queryable = database.Projects
            .Include(project => project.Members)
            .Where(project => project.IsPublished || project.Members.Any(member => member.UserId == userId))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(p =>
                EF.Functions.ILike(p.Name, $"%{query}%") ||
                (p.Summary != null && EF.Functions.ILike(p.Summary, $"%{query}%")));
        }

        if (fromUserId is not null)
        {
            queryable = queryable.Where(project => project.Members.Any(member => member.UserId == fromUserId));
        }
        
        queryable = order switch
        {
            ProjectOrder.Relevance => queryable.OrderByDescending(project => project.CreatedAt),
            ProjectOrder.Published => queryable.OrderByDescending(project => project.PublishedAt),
            ProjectOrder.Updated => queryable.OrderByDescending(project => project.UpdatedAt),
            _ => queryable
        };
        
        var projects = await queryable
            .Select(p => new Project
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Summary = p.Summary,
                ImageUrl = p.ImageUrl
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var totalCount = await queryable.CountAsync();

        return new PagedResult<Project>
        {
            Data = projects,
            Success = true,
            Message = "Projects retrieved successfully",
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
        
        
    }

    public async Task<Project?> GetById(Ulid id, string? userId = null, bool force = false)
    {
        return await database.Projects
            .Include(project => project.Members)
            .Where(project => project.Id == id)
            .FirstOrDefaultAsync(project =>
                force || project.IsPublished || project.Members.Any(member => member.UserId == userId));
    }

    public async Task<Project?> GetBySlug(string slug, string? userId = null, bool force = false)
    {
        return await database.Projects
            .Include(project => project.Members)
            .Where(project => project.Slug == slug)
            .FirstOrDefaultAsync(project =>
                force || project.IsPublished || project.Members.Any(member => member.UserId == userId));
    }

    public async Task<Project?> Create(Project project)
    {
        var createdProject = await database.Projects.AddAsync(project);
        await database.SaveChangesAsync();
        return createdProject.Entity;
    }
}