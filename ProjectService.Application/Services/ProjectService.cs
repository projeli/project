using System.Text.RegularExpressions;
using AutoMapper;
using ProjectService.Application.Dtos;
using ProjectService.Application.Services.Interfaces;
using ProjectService.Domain.Models;
using ProjectService.Domain.Repositories;
using Projeli.Shared.Application.Exceptions.Http;
using Projeli.Shared.Domain.Results;

namespace ProjectService.Application.Services;

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

    public async Task<IResult<ProjectDto?>> Create(ProjectDto projectDto, string userId)
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
        if (projectDto.IsPublished)
        {
            projectDto.PublishedAt = DateTime.UtcNow;
        }

        projectDto.Tags.ForEach(tag => tag.Id = Ulid.NewUlid());

        var validationResult = await ValidateProject(projectDto);
        if (validationResult.Failed) return validationResult;

        var project = mapper.Map<Project>(projectDto);
        var createdProject = await repository.Create(project);

        return createdProject is not null
            ? new Result<ProjectDto?>(mapper.Map<ProjectDto>(createdProject))
            : Result<ProjectDto?>.Fail("Failed to create project");
    }

    public async Task<IResult<ProjectDto?>> Update(Ulid id, ProjectDto projectDto, string userId)
    {
        var existingProject = await repository.GetById(id, userId);
        if (existingProject is null) return Result<ProjectDto>.NotFound();

        var member = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (member is null || !member.IsOwner || !member.Permissions.HasFlag(ProjectMemberPermissions.EditProject))
        {
            throw new ForbiddenException("You do not have permission to edit this project");
        }
        
        projectDto.Id = existingProject.Id;
        projectDto.CreatedAt = existingProject.CreatedAt;
        projectDto.UpdatedAt = DateTime.UtcNow;
        projectDto.Tags.ForEach(tag => tag.Id = Ulid.NewUlid());
        
        if (projectDto.IsPublished && (!existingProject.IsPublished || existingProject.PublishedAt is null))
        {
            projectDto.PublishedAt = DateTime.UtcNow;
        }

        var validationResult = await ValidateProject(projectDto);
        if (validationResult.Failed) return validationResult;

        var project = mapper.Map<Project>(projectDto);
        var updatedProject = await repository.Update(project);

        return updatedProject is not null
            ? new Result<ProjectDto>(mapper.Map<ProjectDto>(updatedProject))
            : Result<ProjectDto>.Fail("Failed to update project");
    }

    private async Task<IResult<ProjectDto?>> ValidateProject(ProjectDto projectDto)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(projectDto.Name))
        {
            errors.Add("name", ["Name is required"]);
        }
        else
        {
            if (projectDto.Name.Length < 3)
            {
                errors.Add("name", ["Name must be at least 3 characters long"]);
            }
            else if (projectDto.Name.Length > 32)
            {
                errors.Add("name", ["Name must be at most 32 characters long"]);
            }
            else if (!NameRegex().IsMatch(projectDto.Name))
            {
                errors.Add("name", ["Name contains invalid characters"]);
            }
        }

        if (string.IsNullOrWhiteSpace(projectDto.Slug))
        {
            errors.Add("slug", ["Slug is required"]);
        }
        else
        {
            if (projectDto.Slug.Length < 3)
            {
                errors.Add("slug", ["Slug must be at least 3 characters long"]);
            }
            else if (projectDto.Slug.Length > 32)
            {
                errors.Add("slug", ["Slug must be at most 32 characters long"]);
            }
            else if (!SlugRegex().IsMatch(projectDto.Slug))
            {
                errors.Add("slug", ["Slug may only contain lowercase letters, numbers, and hyphens"]);
            }
            else
            {
                var existingProject = await repository.GetBySlug(projectDto.Slug, force: true);

                if (existingProject is not null && existingProject.Id != projectDto.Id)
                {
                    errors.Add("slug", ["A project with this slug already exists"]);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(projectDto.Summary))
        {
            if (projectDto.Summary.Length < 32)
            {
                errors.Add("summary", ["Summary must be at least 32 characters long"]);
            }
            else if (projectDto.Summary.Length > 128)
            {
                errors.Add("summary", ["Summary must be at most 128 characters long"]);
            }
            else if (!SummaryRegex().IsMatch(projectDto.Summary))
            {
                errors.Add("summary", ["Summary contains invalid characters"]);
            }
        }

        if (!Enum.IsDefined(typeof(ProjectCategory), projectDto.Category))
        {
            errors.Add("category", ["Category is invalid"]);
        }

        if (projectDto.Tags.Count > 1)
        {
            var tagErrors = new List<string>();
            foreach (var tag in projectDto.Tags)
            {
                if (tag.Name.Length < 2)
                {
                    tagErrors.Add($"Tag '{tag.Name}' must be at least 2 characters long");
                    break;
                }

                if (tag.Name.Length > 24)
                {
                    tagErrors.Add($"Tag '{tag.Name}' must be at most 24 characters long");
                    break;
                }

                if (!TagRegex().IsMatch(tag.Name))
                {
                    tagErrors.Add($"Tag '{tag.Name}' contains invalid characters");
                    break;
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