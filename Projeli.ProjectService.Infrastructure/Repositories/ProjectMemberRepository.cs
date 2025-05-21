using MassTransit;
using Microsoft.EntityFrameworkCore;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.ProjectService.Infrastructure.Database;
using Projeli.Shared.Infrastructure.Messaging.Events;

namespace Projeli.ProjectService.Infrastructure.Repositories;

public class ProjectMemberRepository(ProjectServiceDbContext database, IBus bus) : IProjectMemberRepository
{
    public async Task<List<ProjectMember>> Get(Ulid projectId, string? performingUserId)
    {
        return await database.ProjectMembers
            .AsNoTracking()
            .Where(member => member.ProjectId == projectId)
            .Where(member => member.Project.Status == ProjectStatus.Published || member.Project.Members.Any(x => x.UserId == performingUserId))
            .ToListAsync();
    }

    public async Task<ProjectMember?> Add(Ulid projectId, ProjectMember projectMember)
    {
        var existingProject = await database.Projects
            .Include(project => project.Members)
            .FirstOrDefaultAsync(project => project.Id == projectId);

        if (existingProject is null) return null;

        if (existingProject.Members.Find(member => member.UserId == projectMember.UserId) is not null)
        {
            return null; // User already exists in the project
        }
        
        existingProject.Members.Add(projectMember);
        var result = await database.SaveChangesAsync();

        if (result > 0)
        {
            await bus.Publish(new ProjectUpdatedEvent
            {
                ProjectId = projectId,
                ProjectName = existingProject.Name,
                ProjectSlug = existingProject.Slug,
                Members = existingProject.Members.Select(member => new ProjectUpdatedEvent.ProjectMember
                {
                    UserId = member.UserId,
                    IsOwner = member.IsOwner
                }).ToList()
            });
        }

        return projectMember;
    }

    public async Task<ProjectMember?> UpdateRole(Ulid projectId, Ulid userId, string role)
    {
        var projectMember = await database.ProjectMembers
            .FirstOrDefaultAsync(member => member.ProjectId == projectId && member.Id == userId);

        if (projectMember is null) return null;

        projectMember.Role = role;
        await database.SaveChangesAsync();

        return projectMember;
    }

    public async Task<ProjectMember?> UpdatePermissions(Ulid projectId, Ulid userId, ProjectMemberPermissions permissions)
    {
        var projectMember = await database.ProjectMembers
            .FirstOrDefaultAsync(member => member.ProjectId == projectId && member.Id == userId);

        if (projectMember is null) return null;

        projectMember.Permissions = permissions;
        await database.SaveChangesAsync();

        return projectMember;
    }

    public async Task<bool> Delete(Ulid projectId, string userId)
    {
        var existingProject = await database.Projects
            .Include(project => project.Members)
            .FirstOrDefaultAsync(project => project.Id == projectId);

        if (existingProject is null) return false;
        
        var projectMember = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (projectMember is null) return false;
        
        existingProject.Members.Remove(projectMember);
        
        var result = await database.SaveChangesAsync();

        if (result > 0)
        {
            await bus.Publish(new ProjectUpdatedEvent
            {
                ProjectId = projectId,
                ProjectName = existingProject.Name,
                ProjectSlug = existingProject.Slug,
                Members = existingProject.Members.Select(member => new ProjectUpdatedEvent.ProjectMember
                {
                    UserId = member.UserId,
                    IsOwner = member.IsOwner
                }).ToList()
            });
        }

        return true;
    }
}