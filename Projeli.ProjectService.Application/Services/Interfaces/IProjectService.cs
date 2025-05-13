using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Application.Dtos;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Application.Services.Interfaces;

public interface IProjectService
{
    Task<PagedResult<ProjectDto>> Get(string query, ProjectOrder order, List<ProjectCategory>? categories, string[]? tags, int page, int pageSize, string? fromUserId = null, string? userId = null);
    Task<IResult<ProjectDto?>> GetById(Ulid id, string? userId = null, bool force = false);
    Task<IResult<ProjectDto?>> GetBySlug(string slug, string? userId = null, bool force = false);
    Task<IResult<ProjectDto?>> Create(ProjectDto projectDto, string userId);
    Task<IResult<ProjectDto?>> UpdateDetails(Ulid id, ProjectDto projectDto, string userId);
    Task<IResult<ProjectDto?>> UpdateContent(Ulid id, string content, string userId);
    Task<IResult<ProjectDto?>> UpdateTags(Ulid id, string[] tags, string userId);
    Task<IResult<ProjectDto?>> UpdateStatus(Ulid id, ProjectStatus status, string userId);
    Task<IResult<ProjectDto?>> UpdateOwnership(Ulid id, string newOwnerUserId, string userId);
    Task<IResult<ProjectDto?>> Delete(Ulid id, string userId);
}