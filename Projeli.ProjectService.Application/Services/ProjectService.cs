using System.Text.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.Shared.Application.Exceptions.Http;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Application.Services;

public partial class ProjectService(
    IProjectRepository repository,
    IMapper mapper
) : IProjectService
{
    public async Task<PagedResult<ProjectDto>> Get(string query, ProjectOrder order, List<ProjectCategory>? categories,
        string[]? tags, int page, int pageSize, string? fromUserId = null, string? userId = null)
    {
        var projectsResult = await repository.Get(query, order, categories, tags, page, Math.Clamp(pageSize, 1, 100),
            fromUserId, userId);
        return new PagedResult<ProjectDto>
        {
            Data = mapper.Map<List<ProjectDto>>(projectsResult.Data),
            Page = projectsResult.Page,
            PageSize = projectsResult.PageSize,
            TotalCount = projectsResult.TotalCount,
            TotalPages = projectsResult.TotalPages
        };
    }

    public async Task<IResult<List<ProjectDto>>> GetByUserId(string userId)
    {
        var projects = await repository.GetByUserId(userId);
        return new Result<List<ProjectDto>>(mapper.Map<List<ProjectDto>>(projects));
    }

    public async Task<IResult<ProjectDto?>> GetById(Ulid id, string? userId = null, bool force = false)
    {
        var project = await repository.GetById(id, userId, force);
        return project is not null
            ? new Result<ProjectDto?>(mapper.Map<ProjectDto>(project))
            : Result<ProjectDto?>.NotFound();
    }

    public async Task<IResult<ProjectDto?>> GetBySlug(string slug, string? userId = null, bool force = false)
    {
        var project = await repository.GetBySlug(slug, userId, force);
        return project is not null
            ? new Result<ProjectDto?>(mapper.Map<ProjectDto>(project))
            : Result<ProjectDto?>.NotFound();
    }

    public async Task<IResult<ProjectDto?>> Create(ProjectDto projectDto, IFormFile image, string userId)
    {
        projectDto.Id = Ulid.NewUlid();
        projectDto.CreatedAt = DateTime.UtcNow;
        projectDto.Members =
        [
            new ProjectMemberDto
            {
                Id = Ulid.NewUlid(),
                ProjectId = projectDto.Id,
                UserId = userId,
                IsOwner = true,
                Role = "Owner",
                Permissions = ProjectMemberPermissions.All
            }
        ];

        var project = mapper.Map<Project>(projectDto);

        var validationResult = await ValidateProject(project);
        if (validationResult.Failed) return validationResult;

        var createdProject = await repository.Create(project);

        if (createdProject is not null)
        {
            await repository.UpdateImage(createdProject.Id, image, userId);
        }

        return createdProject is not null
            ? new Result<ProjectDto?>(mapper.Map<ProjectDto>(createdProject))
            : Result<ProjectDto?>.Fail("Failed to create project");
    }

    public async Task<IResult<ProjectDto?>> UpdateDetails(Ulid id, ProjectDto projectDto, string userId)
    {
        var existingProject = await repository.GetById(id, userId);
        if (existingProject is null) return Result<ProjectDto>.NotFound();

        var member = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (member is null || (!member.IsOwner && !member.Permissions.HasFlag(ProjectMemberPermissions.EditProject)))
        {
            throw new ForbiddenException("You do not have permission to edit this project");
        }


        existingProject.Name = projectDto.Name;
        existingProject.Slug = projectDto.Slug;
        existingProject.Summary = projectDto.Summary;
        existingProject.Category = projectDto.Category;

        // Makes sure people aren't going to spam the updated at field to get their project to the top of the list
        existingProject.UpdatedAt = existingProject.UpdatedAt > DateTime.UtcNow.AddDays(-1)
            ? existingProject.UpdatedAt
            : DateTime.UtcNow;

        var validationResult = await ValidateProject(existingProject);
        if (validationResult.Failed) return validationResult;

        var updatedProject = await repository.Update(existingProject);

        return updatedProject is not null
            ? new Result<ProjectDto>(mapper.Map<ProjectDto>(updatedProject))
            : Result<ProjectDto>.Fail("Failed to update project");
    }

    public async Task<IResult<ProjectDto?>> UpdateContent(Ulid id, string content, string userId)
    {
        var existingProject = await repository.GetById(id, userId);
        if (existingProject is null) return Result<ProjectDto>.NotFound();

        var member = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (member is null || (!member.IsOwner && !member.Permissions.HasFlag(ProjectMemberPermissions.EditProject)))
        {
            throw new ForbiddenException("You do not have permission to edit this project");
        }

        existingProject.Content = content;

        // Makes sure people aren't going to spam the updated at field to get their project to the top of the list
        existingProject.UpdatedAt = existingProject.UpdatedAt > DateTime.UtcNow.AddDays(-1)
            ? existingProject.UpdatedAt
            : DateTime.UtcNow;

        var updatedProject = await repository.Update(existingProject);

        return updatedProject is not null
            ? new Result<ProjectDto>(mapper.Map<ProjectDto>(updatedProject))
            : Result<ProjectDto>.Fail("Failed to update project");
    }

    public async Task<IResult<ProjectDto?>> UpdateTags(Ulid id, string[] tags, string userId)
    {
        var existingProject = await repository.GetById(id, userId);
        if (existingProject is null) return Result<ProjectDto>.NotFound();

        var member = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (member is null || (!member.IsOwner && !member.Permissions.HasFlag(ProjectMemberPermissions.EditProject)))
        {
            throw new ForbiddenException("You do not have permission to edit this project");
        }

        existingProject.Tags = tags.Select(tag => new ProjectTag { Name = tag, Id = Ulid.NewUlid() }).ToList();

        var validationResult = await ValidateProject(existingProject);
        if (validationResult.Failed) return validationResult;

        var updatedProject = await repository.Update(existingProject);

        return updatedProject is not null
            ? new Result<ProjectDto>(mapper.Map<ProjectDto>(updatedProject))
            : Result<ProjectDto>.Fail("Failed to update project");
    }

    public async Task<IResult<ProjectDto?>> UpdateStatus(Ulid id, ProjectStatus status, string userId)
    {
        var existingProject = await repository.GetById(id, userId);
        if (existingProject is null) return Result<ProjectDto>.NotFound();

        var member = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (member is null || (!member.IsOwner && !member.Permissions.HasFlag(ProjectMemberPermissions.PublishProject)))
        {
            throw new ForbiddenException("You do not have permission to edit this project");
        }

        switch (status)
        {
            case ProjectStatus.Draft:
                return Result<ProjectDto>.Fail("Cannot set status to draft");
            case ProjectStatus.Review:
                return Result<ProjectDto>.Fail("Cannot set status to review"); //TODO: Add review logic
            case ProjectStatus.Published when existingProject.Status != ProjectStatus.Draft &&
                                              existingProject.Status != ProjectStatus.Archived:
                return Result<ProjectDto>.Fail("Cannot set status to published");
            case ProjectStatus.Archived when existingProject.Status != ProjectStatus.Published:
                return Result<ProjectDto>.Fail("Cannot set status to archived");
        }

        var updatedProject = await repository.UpdateStatus(id, status);

        return updatedProject is not null
            ? new Result<ProjectDto>(mapper.Map<ProjectDto>(updatedProject))
            : Result<ProjectDto>.Fail("Failed to update project");
    }

    public async Task<IResult<ProjectDto?>> UpdateOwnership(Ulid id, string newOwnerUserId, string userId)
    {
        var existingProject = await repository.GetById(id, userId);
        if (existingProject is null) return Result<ProjectDto>.NotFound();

        var owner = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (owner is null || !owner.IsOwner)
        {
            throw new ForbiddenException("You do not have permission to transfer ownership of this project");
        }

        var newOwner = existingProject.Members.FirstOrDefault(x => x.UserId == newOwnerUserId);
        if (newOwner is null)
        {
            return Result<ProjectDto>.Fail("New owner does not exist in the project");
        }

        var oldOwnerPermissions = Enum.GetValues(typeof(ProjectMemberPermissions))
            .Cast<ProjectMemberPermissions>()
            .Where(p => p != ProjectMemberPermissions.All)
            .Aggregate(ProjectMemberPermissions.None, (current, permission) => current | permission);

        var updatedProject =
            await repository.UpdateOwnership(id, owner.Id, newOwner.Id, oldOwnerPermissions,
                ProjectMemberPermissions.All);

        return updatedProject is not null
            ? new Result<ProjectDto>(mapper.Map<ProjectDto>(updatedProject))
            : Result<ProjectDto>.Fail("Failed to update project");
    }

    public async Task<IResult<ProjectDto?>> UpdateImageUrl(Ulid projectId, string filePath, string userId)
    {
        var existingProject = await repository.GetById(projectId, userId);
        if (existingProject is null) return Result<ProjectDto>.NotFound();

        var updatedProject = await repository.UpdateImageUrl(existingProject.Id, filePath);

        return updatedProject is not null
            ? new Result<ProjectDto>(mapper.Map<ProjectDto>(updatedProject))
            : Result<ProjectDto>.Fail("Failed to update project");
    }

    public async Task<IResult<ProjectDto?>> UpdateImage(Ulid id, IFormFile image, string userId)
    {
        var existingProject = await repository.GetById(id, userId);
        if (existingProject is null) return Result<ProjectDto>.NotFound();

        var member = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (member is null || (!member.IsOwner && !member.Permissions.HasFlag(ProjectMemberPermissions.EditProject)))
        {
            throw new ForbiddenException("You do not have permission to edit this project");
        }

        if (image.Length < 1 * 1024)
        {
            return Result<ProjectDto>.Fail("Image must be at least 1KB");
        }

        if (image.Length > 2 * 1024 * 1024)
        {
            return Result<ProjectDto>.Fail("Image must be at most 2MB");
        }

        var imageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!imageTypes.Contains(image.ContentType))
        {
            return Result<ProjectDto>.Fail("Image must be a JPEG, PNG, GIF or WEBP");
        }

        await repository.UpdateImage(id, image, userId);

        return new Result<ProjectDto?>(mapper.Map<ProjectDto>(existingProject));
    }

    public async Task<IResult<ProjectDto?>> Delete(Ulid id, string userId)
    {
        var existingProject = await repository.GetById(id, userId);
        if (existingProject is null) return Result<ProjectDto>.NotFound();

        var member = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (member is null || (!member.IsOwner && !member.Permissions.HasFlag(ProjectMemberPermissions.DeleteProject)))
        {
            throw new ForbiddenException("You do not have permission to delete this project");
        }

        var success = await repository.Delete(id, userId);
        return success
            ? new Result<ProjectDto?>(mapper.Map<ProjectDto>(existingProject))
            : Result<ProjectDto?>.Fail("Failed to delete project");
    }

    private async Task<IResult<ProjectDto?>> ValidateProject(Project project)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(project.Name))
        {
            errors.Add("name", ["Name is required"]);
        }
        else
        {
            if (project.Name.Length < 3)
            {
                errors.Add("name", ["Name must be at least 3 characters long"]);
            }
            else if (project.Name.Length > 32)
            {
                errors.Add("name", ["Name must be at most 32 characters long"]);
            }
            else if (!NameRegex().IsMatch(project.Name))
            {
                errors.Add("name", ["Name contains invalid characters"]);
            }
        }

        if (string.IsNullOrWhiteSpace(project.Slug))
        {
            errors.Add("slug", ["Slug is required"]);
        }
        else
        {
            if (project.Slug.Length < 3)
            {
                errors.Add("slug", ["Slug must be at least 3 characters long"]);
            }
            else if (project.Slug.Length > 32)
            {
                errors.Add("slug", ["Slug must be at most 32 characters long"]);
            }
            else if (!SlugRegex().IsMatch(project.Slug))
            {
                errors.Add("slug", ["Slug may only contain lowercase letters, numbers, and hyphens"]);
            }
            else
            {
                var existingProject = await repository.GetBySlug(project.Slug, force: true);

                if (existingProject is not null && existingProject.Id != project.Id)
                {
                    errors.Add("slug", ["A project with this slug already exists"]);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(project.Summary))
        {
            if (project.Summary.Length < 32)
            {
                errors.Add("summary", ["Summary must be at least 32 characters long"]);
            }
            else if (project.Summary.Length > 128)
            {
                errors.Add("summary", ["Summary must be at most 128 characters long"]);
            }
            else if (!SummaryRegex().IsMatch(project.Summary))
            {
                errors.Add("summary", ["Summary contains invalid characters"]);
            }
        }

        if (!Enum.IsDefined(typeof(ProjectCategory), project.Category) || project.Category == ProjectCategory.None)
        {
            errors.Add("category", ["Category is invalid"]);
        }

        if (project.Tags.Count > 5)
        {
            errors.Add("tags", ["A project may have at most 5 tags"]);
        }
        else if (project.Tags.Count > 0)
        {
            var tagErrors = new List<string>();
            foreach (var tag in project.Tags)
            {
                if (tag.Name.Length < 2)
                {
                    tagErrors.Add($"Tag '{tag.Name}' must be at least 2 characters long");
                    continue;
                }

                if (tag.Name.Length > 24)
                {
                    tagErrors.Add($"Tag '{tag.Name}' must be at most 24 characters long");
                    continue;
                }

                if (!TagRegex().IsMatch(tag.Name))
                {
                    tagErrors.Add($"Tag '{tag.Name}' contains invalid characters");
                }
            }

            if (tagErrors.Count > 0)
            {
                errors.Add("tags", tagErrors.ToArray());
            }
        }

        return errors.Count > 0
            ? Result<ProjectDto?>.ValidationFail(errors)
            : new Result<ProjectDto?>(null);
    }

    [GeneratedRegex(@"^[\w\s\.,!?'""()&+\-*/\\:;@%<>=|{}\[\]^~]{3,32}$")]
    public static partial Regex NameRegex();

    [GeneratedRegex(@"^[a-z0-9-]{3,32}$")]
    public static partial Regex SlugRegex();

    [GeneratedRegex(@"^[\w\s\.,!?'""()&+\-*/\\:;@%<>=|{}\[\]^~]{32,128}$")]
    public static partial Regex SummaryRegex();

    [GeneratedRegex(@"^[a-z-]{2,24}$")]
    public static partial Regex TagRegex();
}