using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Domain.Repositories;

public interface IProjectMemberRepository
{
    Task<List<ProjectMember>> Get(Ulid projectId, string? performingUserId);
    Task<ProjectMember?> Add(Ulid projectId, ProjectMember projectMember);
    Task<ProjectMember?> UpdateRole(Ulid projectId, Ulid userId, string role);
    Task<ProjectMember?> UpdatePermissions(Ulid projectId, Ulid userId, ProjectMemberPermissions permissions);
    Task<bool> Delete(Ulid projectId, string userId);
}