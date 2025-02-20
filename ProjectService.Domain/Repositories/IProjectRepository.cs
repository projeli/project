using ProjectService.Domain.Models;
using ProjectService.Domain.Results;

namespace ProjectService.Domain.Repositories;

public interface IProjectRepository
{
    Task<PagedResult<Project>> Get(string query, ProjectOrder order, int page, int pageSize, string? fromUserId = null, string? userId = null);
    Task<Project?> GetById(Ulid id, string? userId = null, bool force = false);
    Task<Project?> GetBySlug(string slug, string? userId = null, bool force = false);
    Task<Project?> Create(Project project);
}