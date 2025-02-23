using ProjectService.Application.Dtos;
using ProjectService.Domain.Models;
using ProjectService.Domain.Results;

namespace ProjectService.Application.Services.Interfaces;

public interface IProjectService
{
    Task<PagedResult<ProjectDto>> Get(string query, ProjectOrder order, List<ProjectCategory>? categories, string[]? tags, int page, int pageSize, string? fromUserId = null, string? userId = null);
    Task<IResult<ProjectDto?>> GetById(Ulid id, string? userId = null);
    Task<IResult<ProjectDto?>> GetBySlug(string slug, string? userId = null);
    Task<IResult<ProjectDto?>> Create(ProjectDto projectDto, string userId);
}