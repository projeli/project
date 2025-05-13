using Microsoft.EntityFrameworkCore;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.ProjectService.Infrastructure.Database;

namespace Projeli.ProjectService.Infrastructure.Repositories;

public class ProjectMemberRepository(ProjectServiceDbContext database) : IProjectMemberRepository
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
        await database.SaveChangesAsync();

        return projectMember;
    }

    public async Task<ProjectMember?> UpdatePermissions(Ulid projectId, string userId, ProjectMemberPermissions permissions)
    {
        var projectMember = await database.ProjectMembers
            .FirstOrDefaultAsync(member => member.ProjectId == projectId && member.UserId == userId);

        if (projectMember is null) return null;

        projectMember.Permissions = permissions;
        await database.SaveChangesAsync();

        return projectMember;
    }

    public async Task<bool> Delete(Ulid projectId, string userId)
    {
        var projectMember = await database.ProjectMembers
            .FirstOrDefaultAsync(member => member.ProjectId == projectId && member.UserId == userId);

        if (projectMember is null) return false;

        database.ProjectMembers.Remove(projectMember);
        await database.SaveChangesAsync();

        return true;
    }
}