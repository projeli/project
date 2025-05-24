using System.Text.RegularExpressions;
using AutoMapper;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.Shared.Application.Exceptions.Http;
using Projeli.Shared.Application.Messages.Notifications;
using Projeli.Shared.Application.Messages.Projects.Members;
using Projeli.Shared.Domain.Models.Notifications;
using Projeli.Shared.Domain.Models.Notifications.Types.Projects;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Application.Services;

public partial class ProjectMemberService(
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository,
    IBusRepository busRepository,
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
        }, userId);

        if (newMember is not null)
        {
            await busRepository.Publish(new ProjectMemberAddedMessage
            {
                ProjectId = projectId,
                UserId = userId,
                PerformingUserId = performingUserId,
            });

            await busRepository.Publish(new AddNotificationMessage
            {
                Notification = new NotificationMessage
                {
                    UserId = userId,
                    Type = NotificationType.ProjectMemberAdded,
                    Body = new ProjectMemberAdded
                    {
                        ProjectId = projectId,
                        PerformerId = performingUserId,
                    },
                    IsRead = false,
                }
            });
        }

        return newMember is not null
            ? new Result<ProjectMemberDto>(mapper.Map<ProjectMemberDto>(newMember))
            : Result<ProjectMemberDto>.Fail("Failed to add project member");
    }

    public async Task<IResult<ProjectMemberDto?>> UpdateRole(Ulid projectId, Ulid userId, string role,
        string performingUserId)
    {
        var existingProject = await projectRepository.GetById(projectId, performingUserId);
        if (existingProject is null) return Result<ProjectMemberDto>.NotFound();

        var performingMember = existingProject.Members.FirstOrDefault(member => member.UserId == performingUserId);
        if (performingMember is null || (!performingMember.IsOwner &&
                                         !performingMember.Permissions.HasFlag(ProjectMemberPermissions
                                             .EditProjectMemberRoles)))
        {
            throw new ForbiddenException("You do not have permission to edit project member roles.");
        }

        var targetMember = existingProject.Members.FirstOrDefault(member => member.Id == userId);

        if (targetMember is null)
        {
            return Result<ProjectMemberDto>.NotFound();
        }

        if (targetMember.IsOwner && !performingMember.IsOwner)
        {
            throw new ForbiddenException(
                "You cannot change the role of the owner of the project.");
        }

        if (role.Length < 3)
        {
            return Result<ProjectMemberDto?>.ValidationFail(new Dictionary<string, string[]>
            {
                { nameof(role), ["Role must be at least 3 characters long."] }
            });
        }

        if (role.Length > 16)
        {
            return Result<ProjectMemberDto?>.ValidationFail(new Dictionary<string, string[]>
            {
                { nameof(role), ["Role must be at most 16 characters long."] }
            });
        }

        if (!RoleRegex().IsMatch(role))
        {
            return Result<ProjectMemberDto?>.ValidationFail(new Dictionary<string, string[]>
            {
                { nameof(role), ["Role can only contain letters, numbers, spaces, and special characters."] }
            });
        }

        targetMember.Role = role;

        var result = await projectMemberRepository.UpdateRole(projectId, userId, role);

        return result is not null
            ? new Result<ProjectMemberDto>(mapper.Map<ProjectMemberDto>(targetMember))
            : Result<ProjectMemberDto>.Fail("Failed to update project member role");
    }

    public async Task<IResult<ProjectMemberDto?>> UpdatePermissions(Ulid projectId, Ulid userId,
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

        var memberToUpdate = existingProject.Members.FirstOrDefault(member => member.Id == userId);

        if (memberToUpdate is null)
        {
            return Result<ProjectMemberDto>.NotFound();
        }

        if (memberToUpdate.IsOwner)
        {
            throw new ForbiddenException(
                "You cannot change the permissions of the owner of the project.");
        }

        var difference = requestPermissions ^ memberToUpdate.Permissions;
        if (!performingMember.IsOwner && difference != ProjectMemberPermissions.None &&
            !performingMember.Permissions.HasFlag(difference))
        {
            throw new ForbiddenException("You can only add permissions that you have.");
        }

        memberToUpdate.Permissions = requestPermissions;

        var result = await projectMemberRepository.UpdatePermissions(projectId, userId, requestPermissions);
        return result is not null
            ? new Result<ProjectMemberDto>(mapper.Map<ProjectMemberDto>(memberToUpdate))
            : Result<ProjectMemberDto>.Fail("Failed to update project member permissions");
    }

    public async Task<IResult<ProjectMemberDto?>> Delete(Ulid projectId, string targetUserId, string? performingUserId,
        bool force = false)
    {
        var existingProject = await projectRepository.GetById(projectId, performingUserId, force);
        if (existingProject is null) return Result<ProjectMemberDto>.NotFound();

        var performingMember = existingProject.Members.FirstOrDefault(member => member.UserId == performingUserId);
        if (!force && (performingMember is null || (!performingMember.IsOwner &&
                                                    !performingMember.Permissions.HasFlag(ProjectMemberPermissions
                                                        .DeleteProjectMembers))))
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

        var success = await projectMemberRepository.Delete(projectId, targetUserId);

        if (success)
        {
            await busRepository.Publish(new ProjectMemberRemovedMessage
            {
                ProjectId = projectId,
                UserId = targetUserId,
                PerformingUserId = performingUserId ?? targetUserId
            });

            await busRepository.Publish(new AddNotificationMessage
            {
                Notification = new NotificationMessage
                {
                    UserId = targetUserId,
                    Type = NotificationType.ProjectMemberRemoved,
                    Body = new ProjectMemberRemoved
                    {
                        ProjectId = projectId,
                    },
                    IsRead = false,
                }
            });
        }

        return success
            ? new Result<ProjectMemberDto>(mapper.Map<ProjectMemberDto>(targetMember))
            : Result<ProjectMemberDto>.Fail("Failed to delete project member");
    }

    [GeneratedRegex(@"^[\w\s\.,!?'""()&+\-*/\\:;@%<>=|{}\[\]^~]{3,16}$")]
    public static partial Regex RoleRegex();
}