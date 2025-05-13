using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Domain.Repositories;

public interface IProjectMemberRepository
{
    Task<List<ProjectMember>> Get(Ulid projectId, string? performingUserId);
    Task<ProjectMember?> Add(Ulid projectId, ProjectMember projectMember);
    Task<ProjectMember?> UpdatePermissions(Ulid projectId, string userId, ProjectMemberPermissions permissions);
    Task<bool> Delete(Ulid projectId, string userId);
}