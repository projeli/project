using AutoMapper;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.Shared.Application.Exceptions.Http;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Application.Services;

public class ProjectMemberService(
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository,
    IMapper mapper) : IProjectMemberService
{
    public async Task<IResult<List<ProjectMemberDto>>> Get(Ulid projectId, string? performingUserId = null)
    {
        var members = await projectMemberRepository.Get(projectId, performingUserId);
        return new Result<List<ProjectMemberDto>>(mapper.Map<List<ProjectMemberDto>>(members));
    }

    public async Task<IResult<ProjectMemberDto?>> Add(Ulid projectId, string userId, string performingUserId)
    {
        var existingProject = await projectRepository.GetById(projectId, performingUserId);
        if (existingProject is null) return Result<ProjectMemberDto>.NotFound();

        var member = existingProject.Members.FirstOrDefault(member => member.UserId == performingUserId);
        if (member is null ||
            (!member.IsOwner && !member.Permissions.HasFlag(ProjectMemberPermissions.AddProjectMembers)))
        {
            throw new ForbiddenException("You do not have permission to add project members.");
        }

        if (existingProject.Members.Count >= 20)
        {
            return Result<ProjectMemberDto>.Fail("Project member limit reached. Cannot add more members.");
        }

        if (existingProject.Members.Any(x => x.UserId == userId))
        {
            return Result<ProjectMemberDto>.Fail("User is already a member of the project.");
        }

        if (userId == performingUserId)
        {
            return Result<ProjectMemberDto>.Fail("You cannot add yourself as a project member.");
        }

        var newMember = await projectMemberRepository.Add(projectId, new ProjectMember
        {
            Id = Ulid.NewUlid(),
            ProjectId = projectId,
            UserId = userId,
            Permissions = ProjectMemberPermissions.None,
            IsOwner = false,
            Role = "Member"
        });

        return newMember is not null
            ? new Result<ProjectMemberDto>(mapper.Map<ProjectMemberDto>(newMember))
            : Result<ProjectMemberDto>.Fail("Failed to add project member");
    }

    public async Task<IResult<ProjectMemberDto?>> Delete(Ulid projectId, string targetUserId, string performingUserId)
    {
        var existingProject = await projectRepository.GetById(projectId, performingUserId);
        if (existingProject is null) return Result<ProjectMemberDto>.NotFound();

        var performingMember = existingProject.Members.FirstOrDefault(member => member.UserId == performingUserId);
        if (performingMember is null || (!performingMember.IsOwner &&
                                         !performingMember.Permissions.HasFlag(ProjectMemberPermissions
                                             .DeleteProjectMembers)))
        {
            throw new ForbiddenException("You do not have permission to delete project members.");
        }

        if (existingProject.Members.Count == 1)
        {
            throw new ForbiddenException("You cannot leave the project as the only member.");
        }

        var targetMember = existingProject.Members.FirstOrDefault(member => member.UserId == targetUserId);

        if (targetMember is null)
        {
            return Result<ProjectMemberDto>.NotFound();
        }

        if (targetMember.IsOwner)
        {
            throw new ForbiddenException(
                "You cannot remove the owner of the project. Please transfer ownership first."
            );
        }

        var result = await projectMemberRepository.Delete(projectId, targetUserId);
        return result
            ? new Result<ProjectMemberDto>(mapper.Map<ProjectMemberDto>(targetMember))
            : Result<ProjectMemberDto>.Fail("Failed to delete project member");
    }

    public async Task<IResult<ProjectMemberDto?>> UpdatePermissions(Ulid projectId, string userId,
        ProjectMemberPermissions requestPermissions,
        string performingUserId)
    {
        var existingProject = await projectRepository.GetById(projectId, performingUserId);
        if (existingProject is null) return Result<ProjectMemberDto>.NotFound();

        var performingMember = existingProject.Members.FirstOrDefault(member => member.UserId == performingUserId);
        if (performingMember is null || (!performingMember.IsOwner &&
                                         !performingMember.Permissions.HasFlag(ProjectMemberPermissions
                                             .EditProjectMemberPermissions)))
        {
            throw new ForbiddenException("You do not have permission to edit project member permissions.");
        }

        var memberToUpdate = existingProject.Members.FirstOrDefault(member => member.UserId == userId);

        if (memberToUpdate is null)
        {
            return Result<ProjectMemberDto>.NotFound();
        }

        if (memberToUpdate.IsOwner)
        {
            throw new ForbiddenException(
                "You cannot change the permissions of the owner of the project. Please transfer ownership first.");
        }

        var difference = requestPermissions ^ memberToUpdate.Permissions;
        if (difference != ProjectMemberPermissions.None && !performingMember.Permissions.HasFlag(difference))
        {
            throw new ForbiddenException("You can only add permissions that you have.");
        }

        memberToUpdate.Permissions = requestPermissions;

        var result = await projectMemberRepository.UpdatePermissions(projectId, userId, requestPermissions);
        return result is not null
            ? new Result<ProjectMemberDto>(mapper.Map<ProjectMemberDto>(memberToUpdate))
            : Result<ProjectMemberDto>.Fail("Failed to update project member permissions");
    }
}