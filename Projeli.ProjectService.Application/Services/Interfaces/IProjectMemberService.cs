using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Domain.Models;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Application.Services.Interfaces;

public interface IProjectMemberService
{
    Task<IResult<List<ProjectMemberDto>>> Get(Ulid projectId, string? performingUserId = null);
    Task<IResult<ProjectMemberDto?>> Add(Ulid projectId, string userId, string performingUserId);

    Task<IResult<ProjectMemberDto?>> UpdateRole(Ulid projectId, Ulid userId, string role,
        string performingUserId);

    Task<IResult<ProjectMemberDto?>> UpdatePermissions(Ulid projectId, Ulid userId,
        ProjectMemberPermissions requestPermissions, string performingUserId);

    Task<IResult<ProjectMemberDto?>> Delete(Ulid projectId, string targetUserId, string? performingUserId, bool force = false);
}