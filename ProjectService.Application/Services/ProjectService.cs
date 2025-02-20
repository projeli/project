using System.Text.RegularExpressions;
using AutoMapper;
using ProjectService.Application.Dtos;
using ProjectService.Application.Services.Interfaces;
using ProjectService.Domain.Models;
using ProjectService.Domain.Repositories;
using ProjectService.Domain.Results;

namespace ProjectService.Application.Services;

public partial class ProjectService(IProjectRepository repository, IMapper mapper) : IProjectService
{
    public async Task<IResult<ProjectDto?>> GetById(Ulid id)
    {
        var project = await repository.GetById(id);
        return project is not null
            ? new Result<ProjectDto?>(mapper.Map<ProjectDto>(project))
            : Result<ProjectDto?>.NotFound();
    }

    public async Task<IResult<ProjectDto?>> GetBySlug(string slug)
    {
        var project = await repository.GetBySlug(slug);
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

        var validationResult = await ValidateProject(projectDto);
        if (validationResult.Failed) return validationResult;

        var project = mapper.Map<Project>(projectDto);
        var createdProject = await repository.Create(project);

        return createdProject is not null
            ? new Result<ProjectDto?>(mapper.Map<ProjectDto>(createdProject))
            : Result<ProjectDto?>.Fail("Failed to create project");
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
                var existingProject = await repository.GetBySlug(projectDto.Slug);

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
}